/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     23-01-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WPFhelper
{

	/// <summary>
	/// Handle a waiting indication adorning a control
	/// </summary>
	public static class WaitingWheel
	{

		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Enable and show the waiting wheel (rotating dots) on the control 
		/// </summary>
		public static readonly DependencyProperty EnableProperty =
		DependencyProperty.RegisterAttached("Enable", typeof(bool),
			typeof(WaitingWheel), new PropertyMetadata(false, EnableChanged));

		public static bool GetEnable(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableProperty);
		}

		public static void SetEnable(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableProperty, value);
		}


		private static Dictionary<DependencyObject, WaitingWheelAdorner> AdornerTable = new Dictionary<DependencyObject, WaitingWheelAdorner>(); 


		private static void EnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxthis = (FrameworkElement)d;

			if ((bool)e.NewValue)
			{
				var adorner = new WaitingWheelAdorner(wxthis);
				AdornerTable.Add(d, adorner);
			}
			else
			{
				if ( AdornerTable.TryGetValue(d, out WaitingWheelAdorner adorner) )
				{
					AdornerTable.Remove(d);
					adorner.Destroy();
				}
			}
		}



		// ----------------------------------------------------------------------------------------------
		/// <summary>
		/// Drag Adorner
		/// </summary>
		public class WaitingWheelAdorner : Adorner
		{
			private Viewbox Visual;
			private AdornerLayer adornerLayer;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="adornedElement"></param>
			public WaitingWheelAdorner(FrameworkElement adornedElement) : base(adornedElement)
			{
				Brush fill = TextBlock.GetForeground(adornedElement);

				if (fill == null)
				{
					fill = Brushes.Red;
				}

				HandleVisual(fill);
				AddVisualChild(Visual);

				adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
				adornerLayer.Add(this);
			}

			/// <summary>
			/// Destructor
			/// </summary>
			public void Destroy()
			{
				HandleVisual(null); // Force animations to stop instantly
				adornerLayer.Remove(this); // will destroy the visual by GC
			}



			/// <summary>
			/// Animate Dot color
			/// </summary>
			/// <param name="fill">brush to use, null to destroy.</param>
			private void HandleVisual(Brush fill)
			{
				// Create Fill gradient from Foreground in the code
				if (fill != null)
				{
					if (fill is SolidColorBrush brush)
					{
						var color = brush.Color;
						fill = new RadialGradientBrush()
						{
							ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation,
							GradientStops = new GradientStopCollection()
							{
								new GradientStop(color, 0),
								new GradientStop(color, 0.5),
								new GradientStop(Colors.Transparent, 1),
							}
						};
					}

					var wxCanvas = new Canvas()
					{
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						Width = 100,
						Height = 100
					};

					Visual = new Viewbox()
					{
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
						Child = wxCanvas

					};


					for (int index = 0; index < 11; index++)
					{
						// Compute dots positions
						Ellipse ellipse = new Ellipse
						{
							Opacity = 0,
							Width = 12,
							Height = 12
						};

						// Size of the Canvas is 100
						const double center = 40.0;
						const double radius = 25.0;
						const double offset = Math.PI / 2.0;
						const double delta = -2.0 * Math.PI / 10.0;

						double angle = offset + index * delta;

						ellipse.SetValue(Canvas.LeftProperty, center + radius * Math.Sin(angle));
						ellipse.SetValue(Canvas.TopProperty, center + radius * Math.Cos(angle));

						wxCanvas.Children.Add(ellipse);


						// Update the Fill property
						ellipse.Fill = fill;

						// Animate dot (color)
						DoubleAnimation anim = null;
						anim = new DoubleAnimation(-1.6, 1, new Duration(new TimeSpan(0, 0, 1)), new FillBehavior())
						{
							AutoReverse = true,
							RepeatBehavior = RepeatBehavior.Forever,
							BeginTime = new TimeSpan(0, 0, 0, 0, index * 100)
						};

						ellipse.BeginAnimation(OpacityProperty, anim);

					}

				}
				else if (Visual != null)
				{
					// Stop animations right now to save CPU
					var wxCanvas = (Canvas)Visual.Child;
					foreach (var ellipse in wxCanvas.Children)
					{
						((Ellipse)ellipse).BeginAnimation(OpacityProperty, null);
					}

					Visual = null;
				}
			}



			protected override int VisualChildrenCount
			{
				get
				{
					return 1;
				}
			}

			protected override Visual GetVisualChild(int index)
			{
				if (index != 0) throw new ArgumentOutOfRangeException();
				return Visual;
			}

			protected override Size MeasureOverride(Size constraint)
			{
				Visual.Measure(constraint);
				return Visual.DesiredSize;
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				Visual.Arrange(new Rect(finalSize));
				return finalSize;
			}

			public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
			{
				GeneralTransformGroup result = new GeneralTransformGroup();
				result.Children.Add(base.GetDesiredTransform(transform));
				return result;
			}
		}


	}
}
