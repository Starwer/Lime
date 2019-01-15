/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     06-01-2019
* Copyright :   Sebastien Mouy © 2019  
**************************************************************************/

using Lime;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFhelper
{
	/// <summary>
	/// System-style Drag & Drop Icon handling (Windows desktop)
	/// </summary>
	public class DragSystemIcon
	{

		// ----------------------------------------------------------------------------------------------
		#region Interop
		// The following enable to keep showing the friendly system drag icon from desktop to application
		// Source: https://blogs.msdn.microsoft.com/adamroot/2008/02/19/shell-style-drag-and-drop-in-net-wpf-and-winforms/
		// See also: https://stackoverflow.com/questions/44466208/how-to-implement-the-common-drag-and-drop-icon-in-winforms?rq=1


		[StructLayout(LayoutKind.Sequential)]
		public struct ShDragImage
		{
			public WPF.Win32Size sizeDragImage;
			public WPF.SysPoint ptOffset;
			public IntPtr hbmpDragImage;
			public int crColorKey;
		}


		[ComImport]
		[Guid("4657278A-411B-11d2-839A-00C04FD918D0")]
		public class DragDropHelper { }


		[ComVisible(true)]
		[Guid("83E07D0D-0C5F-4163-BF1A-60B274051E40")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface IDragSourceHelper2
		{
			void InitializeFromBitmap(
				[In, MarshalAs(UnmanagedType.Struct)] ref ShDragImage dragImage,
				[In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject dataObject);

			void InitializeFromWindow(
				[In] IntPtr hwnd,
				[In] ref WPF.SysPoint pt,
				[In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject dataObject);

			/// <summary>
			/// IDragSourceHelper2 extension to IDragSourceHelper: Set flags to allow drop description
			/// </summary>
			/// <param name="dwFlags">Must be 0 (none) or 1 (DSH_ALLOWDROPDESCRIPTIONTEXT)</param>
			void SetFlags(
				[In] int dwFlags
				);
		}


		[ComVisible(true)]
		[ComImport]
		[Guid("4657278B-411B-11D2-839A-00C04FD918D0")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IDropTargetHelper
		{
			void DragEnter(
				[In] IntPtr hwndTarget,
				[In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject dataObject,
				[In] ref WPF.SysPoint pt,
				[In] DragDropEffects effect);

			void DragLeave();


			void DragOver(
				[In] ref WPF.SysPoint pt,
				[In] DragDropEffects effect);


			void Drop(
				[In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject dataObject,
				[In] ref WPF.SysPoint pt,
				[In] DragDropEffects effect);


			void Show(
				[In] bool show);

		}


		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Fields


		private IDropTargetHelper DDHelper;
		private readonly IDataObject ComData;
		private readonly FrameworkElement Element;
		private readonly double Scale;
		public DragDropEffects Effects;

		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Impelmentation


		/// <summary>
		/// Create or handle a system-style Drag & Drop icon
		/// </summary>
		/// <param name="element">Element to represent or where the Drag & Drop operation occurs</param>
		/// <param name="dataObject">Data Object dragged during Drag & Drop</param>
		/// <param name="effects">Drag & Drop effects</param>
		/// <param name="scale">Scale of the element representation when creating icon (1.0: unchanged)</param>
		/// <param name="create">create the icon representation from the element, otherwise tries to continue the existing Drag & Drop from the dataObject</param>
		public DragSystemIcon(FrameworkElement element, IDataObject dataObject, DragDropEffects effects, double scale, bool create)
		{

			// retrieve window handle
			var window = PresentationSource.FromVisual(element).RootVisual;
			var hwnd = ((HwndSource)PresentationSource.FromVisual(window)).Handle;

			if (create)
			{
				Element = element;

				if (element is ContentPresenter)
				{
					element = WPF.FindFirstChild<FrameworkElement>(element);
				}

				// Convert the visual element to a Bitmap
				var dpi = 96 * scale;
				var renderbmp = WPF.ProduceImageSourceForVisual(element, dpi, dpi);

				// Retrieve renderbmp pixel array
				const int pixsize = 4; // Pbgra32 implies 4 bytes per pixels
				int stride = renderbmp.PixelWidth * pixsize;
				byte[] pix = new byte[renderbmp.PixelHeight * stride];
				renderbmp.CopyPixels(pix, stride, 0);

				for (int i = 3; i < pix.Length; i += pixsize)
				{
					if (pix[i] == 0)
					{
						// Convert fully transparent pixels to Magenta (this will become transparent in ShDragImage)
						pix[i - 0] = 0xFF; // A
						pix[i - 1] = 0xFF; // R
						pix[i - 2] = 0x00; // G
						pix[i - 3] = 0xFF; // B
					}
					else if (pix[i] == 0xFF && pix[i - 1] == 0xFF && pix[i - 2] == 0x00 && pix[i - 3] == 0xFF)
					{
						// change Magenta pixels to *almost* magenta, to avoid changing these to transparent in ShDragImage
						pix[i - 2] = 0x01;
					}
				}

				// Convert pixel array to BitmapSource
				var bitmapsrc = BitmapSource.Create(renderbmp.PixelWidth, renderbmp.PixelHeight,
													  96, 96, PixelFormats.Bgra32, null, pix, stride);

				// Convert BitmapSource to Bitmap
				System.Drawing.Bitmap bitmap;
				using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
				{
					var encoder = new BmpBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(bitmapsrc));
					encoder.Save(stream);
					bitmap = new System.Drawing.Bitmap(stream);

				}

				//LimeMsg.Debug("ClipDragDrop DragSystemIcon: {0}", bitmap.GetPixel(0, 0));

				// Compute destination size
				WPF.Win32Size size;
				size.cx = (int)(renderbmp.PixelWidth * scale);
				size.cy = (int)(renderbmp.PixelHeight * scale);

				WPF.SysPoint wpt;
				wpt.x = size.cx / 2;
				wpt.y = size.cy / 2;

				ShDragImage shdi = new ShDragImage
				{
					sizeDragImage = size,
					ptOffset = wpt,
					hbmpDragImage = bitmap.GetHbitmap(),
					crColorKey = System.Drawing.Color.Magenta.ToArgb()
				};


				var sourceHelper = (IDragSourceHelper2)new DragDropHelper();
				sourceHelper.SetFlags(1); // Enable Drop description

				// Not quite right
				var com = new ComDataObject();
				dataObject = new DataObject(com);

				sourceHelper.InitializeFromBitmap(ref shdi, (System.Runtime.InteropServices.ComTypes.IDataObject)dataObject);

			}
			else
			{
				Element = null;
			}

			ComData = dataObject;
			Effects = effects;
			Scale = scale;


			// Create the System Drag Helper and show it at the mouse location 
			WPF.GetCursorPos(out WPF.SysPoint spos);
			DDHelper = (IDropTargetHelper)new DragDropHelper();
			DDHelper.DragEnter(hwnd,
				(System.Runtime.InteropServices.ComTypes.IDataObject)dataObject,
				ref spos, Effects);


		}


		/// <summary>
		/// Destroy the Drag Icon visual
		/// </summary>
		public void Destroy()
		{
			DDHelper.DragLeave();
			Mouse.OverrideCursor = null;
		}

		/// <summary>
		/// Bring the Drag Icon visual at the mouse position
		/// </summary>
		public void UpdatePosition()
		{
			if (WPF.GetCursorPos(out WPF.SysPoint spos))
			{
				DDHelper.DragOver(ref spos, Effects);
			}
		}


		/// <summary>
		/// Update the displayed drag drop effects
		/// </summary>
		/// <param name="effects">Drag & Drop effect to be applied</param>
		/// <param name="type">DropImageType effect to be used</param>
		/// <param name="format">format to be displayed on the icon</param>
		/// <param name="destination">drop destination name</param>
		public void UpdateEffects(DragDropEffects effects, DropImageType type, string format, string destination)
		{
			Effects = effects;
			if (ComData != null)
			{
				// Size of insert is limited to 259
				if (destination != null && destination.Length > 259)
				{
					destination = destination.Substring(255) + "...";
				}

				// C# format to C format
				format = format?.Replace("{0}", "%1");

				// All these steps are required to update the Drop-description
				ComDataObject.SetDropDescription(ComData, type, format, destination);
				ComDataObject.InvalidateDragImage(ComData);
				UpdatePosition(); // force a DDHelper.DragOver call
			}
		}



		/// <summary>
		/// Bring the Drag Icon visual at a given screen position, using animation
		/// </summary>
		/// <param name="position">screen position</param>
		/// <param name="duration">duration of the trip</param>
		/// <param name="destroy">destroy the DragAdorner when done.</param>
		public void GotoPosition(Point position, Duration duration, bool destroy = true)
		{
			// Create a DragAdorner to handle the animation
			if (Element != null && WPF.GetCursorPos(out WPF.SysPoint spos))
			{
				var adorner = new FreeFrameworkElementAvatar(Element, Scale, 0.75);
				adorner.GotoPosition(new Point(spos.x, spos.y));
				adorner.GotoPosition(position, duration, true);
			}

			if (destroy)
			{
				Destroy();
			}
		}


		#endregion
	}

}
