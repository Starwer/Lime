/**************************************************************************
* Author:       Douglas Stockwell
* Website:      http://www.11011.net/archives/000653.html
*  17-11-2015 - Changed by Sebastien Mouy (Starwer)  
*               Added the "Scale" Property
*  22-03-2016 - Changed by Sebastien Mouy (Starwer)  
*               Fix: Position/size of the thumbnail is now properly convert to pixel
**************************************************************************/

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using WPFhelper;

namespace Thumbnail
{
    public class Thumbnail : FrameworkElement
    {


        // ----------------------------------------------------------------------------------------------
        #region Interops

        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr source, out IntPtr hthumbnail);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr HThumbnail);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr HThumbnail, ref ThumbnailProperties props);

        [DllImport("dwmapi.dll")]
        public static extern int DwmQueryThumbnailSourceSize(IntPtr HThumbnail, out WPF.SysSize size);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref WPF.SysRect lpRect);


        #endregion

        // ----------------------------------------------------------------------------------------------

        #region Types

        public struct ThumbnailProperties
        {
            public ThumbnailFlags Flags;
            public WPF.SysRect Destination;
            public WPF.SysRect Source;
            public Byte Opacity;
            public bool Visible;
            public bool SourceClientAreaOnly;
        }


        [Flags]
        public enum ThumbnailFlags : int
        {
            RectDestination = 1,
            RectSource = 2,
            Opacity = 4,
            Visible = 8,
            SourceClientAreaOnly = 16
        }


        #endregion

        // ----------------------------------------------------------------------------------------------



        public Thumbnail()
        {
            this.LayoutUpdated += new EventHandler(Thumbnail_LayoutUpdated);
            this.Unloaded += new RoutedEventHandler(Thumbnail_Unloaded);
        }

        public static DependencyProperty SourceProperty;
        public static DependencyProperty ClientAreaOnlyProperty;
        public static DependencyProperty ScaleProperty;


		static Thumbnail()
        {
            SourceProperty = DependencyProperty.Register(
                "Source",
                typeof(IntPtr),
                typeof(Thumbnail),
                new FrameworkPropertyMetadata(
                    IntPtr.Zero,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    delegate(DependencyObject obj, DependencyPropertyChangedEventArgs args)
                    {
                        ((Thumbnail)obj).InitialiseThumbnail((IntPtr)args.NewValue);
                    }));

            ScaleProperty = DependencyProperty.Register(
                "Scale",
                typeof(double),
                typeof(Thumbnail),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    delegate(DependencyObject obj, DependencyPropertyChangedEventArgs args)
                    {
                        ((Thumbnail)obj).UpdateThumbnail();
                    }));

            ClientAreaOnlyProperty = DependencyProperty.Register(
                "ClientAreaOnly",
                typeof(bool),
                typeof(Thumbnail),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    delegate(DependencyObject obj, DependencyPropertyChangedEventArgs args)
                    {
                        ((Thumbnail)obj).UpdateThumbnail();
                    }));

            OpacityProperty.OverrideMetadata(
                typeof(Thumbnail),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.Inherits,
                    delegate(DependencyObject obj, DependencyPropertyChangedEventArgs args)
                    {
                        ((Thumbnail)obj).UpdateThumbnail();
                    }));
        }

        public IntPtr Source
        {
            get { return (IntPtr)this.GetValue(SourceProperty); }
            set { this.SetValue(SourceProperty, value); }
        }

        public double Scale
        {
            get { return (double)this.GetValue(ScaleProperty); }
            set { this.SetValue(ScaleProperty, value); }
        }

        public bool ClientAreaOnly
        {
            get { return (bool)this.GetValue(ClientAreaOnlyProperty); }
            set { this.SetValue(ClientAreaOnlyProperty, value); }
        }

        public new double Opacity
        {
            get { return (double)this.GetValue(OpacityProperty); }
            set { this.SetValue(OpacityProperty, value); }
        }

        private HwndSource target;
        private IntPtr thumb;

        private void InitialiseThumbnail(IntPtr source)
        {
            if (IntPtr.Zero != thumb)
            {
                // release the old thumbnail
                ReleaseThumbnail();
            }

            if (IntPtr.Zero != source)
            {
                // find our parent hwnd
                target = (HwndSource)HwndSource.FromVisual(this);

                // if we have one, we can attempt to register the thumbnail
                if (target != null && 0 == DwmRegisterThumbnail(target.Handle, source, out this.thumb))
                {
                    ThumbnailProperties props = new ThumbnailProperties();
                    props.Visible = this.Visibility == Visibility.Visible;
                    props.SourceClientAreaOnly = this.ClientAreaOnly;
                    props.Opacity = (byte)(255 * this.Opacity);
                    props.Flags = ThumbnailFlags.Visible | ThumbnailFlags.SourceClientAreaOnly | ThumbnailFlags.Opacity;
                    DwmUpdateThumbnailProperties(thumb, ref props);
                }
            }
        }

        private void ReleaseThumbnail()
        {
            DwmUnregisterThumbnail(thumb);
            this.thumb = IntPtr.Zero;
            this.target = null;
        }

        private void UpdateThumbnail()
        {
            if (IntPtr.Zero != thumb)
            {
				ThumbnailProperties props = new ThumbnailProperties
				{
					Visible = this.Visibility == Visibility.Visible,
					SourceClientAreaOnly = this.ClientAreaOnly,
					Opacity = (byte)(255 * this.Opacity),
					Flags = ThumbnailFlags.Visible | ThumbnailFlags.SourceClientAreaOnly | ThumbnailFlags.Opacity
				};
				DwmUpdateThumbnailProperties(thumb, ref props);
            }
        }

        private void Thumbnail_Unloaded(object sender, RoutedEventArgs e)
        {
            ReleaseThumbnail();
        }

        // this is where the magic happens
        private void Thumbnail_LayoutUpdated(object sender, EventArgs e)
        {
            if (IntPtr.Zero == thumb)
            {
                InitialiseThumbnail(this.Source);
            }

            if (IntPtr.Zero != thumb)
            {
                if (!target.RootVisual.IsAncestorOf(this))
                {
                    //we are no longer in the visual tree
                    ReleaseThumbnail();
                    return;
                }

				DwmQueryThumbnailSourceSize(this.thumb, out WPF.SysSize size);

				Rect visib = WPF.GetVisibleArea(this);

				ThumbnailProperties props = new ThumbnailProperties();

				if (visib == Rect.Empty)
                {
                    props.Visible = false;
                    props.Flags = ThumbnailFlags.Visible;
                }
                else
                {
                    double ratioSrc = (double)size.Width / (double)size.Height;
                    double ratioDest = visib.Width / visib.Height;

                    // Center the thumbnail in the visible area
                    double newSize;
                    if (ratioSrc > ratioDest)
                    {
                        newSize = visib.Height * ratioDest / ratioSrc;
                        visib.Y += (visib.Height - newSize) / 2.0;
                        visib.Height = newSize;
                    }
                    else
                    {
                        newSize = visib.Width * ratioSrc / ratioDest;
                        visib.X += (visib.Width - newSize) / 2.0;
                        visib.Width = newSize;
                    }

                    // Translate to screen pixels
                    Point a = target.RootVisual.PointToScreen(visib.TopLeft);
                    Point b = target.RootVisual.PointToScreen(visib.BottomRight);
                    Point w = Application.Current.MainWindow.PointToScreen(new Point(0, 0));

                    props.Destination = new WPF.SysRect(
                        (int)Math.Ceiling(a.X - w.X), (int)Math.Ceiling(a.Y - w.Y),
                        (int)Math.Ceiling(b.X - w.X), (int)Math.Ceiling(b.Y - w.Y));

                    props.Visible = this.Visibility == Visibility.Visible;
                    props.Flags = ThumbnailFlags.Visible | ThumbnailFlags.RectDestination;
                }


                DwmUpdateThumbnailProperties(thumb, ref props);

            }
        }


		//protected override void OnRender(DrawingContext drawingContext)
		//{
		//	base.OnRender(drawingContext);
		//	Thumbnail_LayoutUpdated(null, null);
		//}


		protected override Size MeasureOverride(Size availableSize)
        {
			WPF.SysSize size;
			DwmQueryThumbnailSourceSize(this.thumb, out size);

			var ret = new Size(
				(Double.IsNaN(availableSize.Width) || Double.IsInfinity(availableSize.Width) ? size.Width : availableSize.Width),
				(Double.IsNaN(availableSize.Height) || Double.IsInfinity(availableSize.Height) ? size.Height : availableSize.Height)
				);

			// Keep Ratio
			if (size.Width > 0 && size.Height > 0)
			{
				double srcRatio = (double)size.Width / (double)size.Height;
				double destRatio = ret.Width / ret.Height;

				if (srcRatio > destRatio)
				{
					ret.Height = ret.Width / srcRatio;
				}
				else if (srcRatio < destRatio)
				{
					ret.Width = ret.Height * srcRatio;
				}
			}
			return ret;

		}

        protected override Size ArrangeOverride(Size finalSize)
        {
			return new Size(finalSize.Width * this.Scale, finalSize.Height * this.Scale);
        }


    }
}