/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     06-01-2019
* Copyright :   Sebastien Mouy © 2019  
**************************************************************************/

using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace WPFhelper
{

	// ----------------------------------------------------------------------------------------------
	/// <summary>
	/// FrameworkElement representation, which can be moved anywhere on the screen
	/// </summary>
	[SupportedOSPlatform("windows")]
	public class FreeFrameworkElementAvatar : IDisposable
	{
		private Popup DragIcon = null;

		/// <summary>
		/// Get the handle of the window
		/// </summary>
		public IntPtr Handle
		{
			get { return ((HwndSource)PresentationSource.FromVisual(DragIcon.Child)).Handle; }
		}

		/// <summary>
		/// Get the image representation of the Drop control
		/// </summary>
		public Image Image { get; private set; }

		/// <summary>
		/// Constructor of the FrameworkElement representation, which can be moved anywhere on the screen
		/// </summary>
		/// <param name="element">Element to create an avatar from</param>
		/// <param name="scale">Scale of the represented avatar compared to its source (default: 1.0: equal size)</param>
		/// <param name="opacity">Opacity of the represented avatar compared to its source (default: 1.0)</param>
		public FreeFrameworkElementAvatar(FrameworkElement element, double scale = 1.0, double opacity = 1.0)
		{
			if (element.GetType() == typeof(ContentPresenter))
			{
				element = WPF.FindFirstChild<FrameworkElement>(element);
			}

			// Create the image
			Image = new Image()
			{
				// Icon Image
				Style = new Style(typeof(Image)),
				Source = WPF.ProduceImageSourceForVisual(element),
				Opacity = opacity,
			};

			// Create the visual
			DragIcon = new Popup()
			{
				AllowsTransparency = true,
				PopupAnimation = PopupAnimation.None,
				Placement = PlacementMode.Absolute,
				Width = element.ActualWidth * scale,
				Height = element.ActualHeight * scale,
				Child = Image
			};
		}

		public void Dispose()
		{
			if (DragIcon != null) DragIcon.IsOpen = false;
		}

		/// <summary>
		/// Bring the avatar at a given screen position
		/// </summary>
		/// <param name="position">screen position</param>
		public void GotoPosition(Point position)
		{
			if (DragIcon != null)
			{
				if (!DragIcon.IsOpen)
				{
					DragIcon.IsOpen = true;

					// Make the icon transparent to the mouse. Unfortunatly, this code 
					// is sytem dependent. Alternative is to change the following icon position
					// to not be under the the mouse (remove the offset for example).
					WPF.SetWindowExTransparent(DragIcon.Child);
				}

				// Icon position with respect to mouse position. 
				// Remove the Y offset if SetWindowExTransparent is not available on the system.
				DragIcon.HorizontalOffset = position.X - DragIcon.Width / 2.0;
				DragIcon.VerticalOffset = position.Y - DragIcon.Height / 2.0;
			}
		}

		/// <summary>
		/// Bring the avatar at a given screen position, using animation
		/// </summary>
		/// <param name="position">screen position</param>
		/// <param name="duration">duration of the trip</param>
		/// <param name="destroy">destroy the avatar when done.</param>
		public void GotoPosition(Point position, Duration duration, bool destroy = false)
		{
			if (DragIcon != null)
			{
				DragIcon.IsOpen = true;

				var animX = new DoubleAnimation(DragIcon.HorizontalOffset,
					position.X - DragIcon.Width / 2, duration, new FillBehavior());
				var animY = new DoubleAnimation(DragIcon.VerticalOffset,
					position.Y - DragIcon.Height / 2, duration, new FillBehavior());

				if (destroy) animX.Completed += AnimX_Completed;

				DragIcon.BeginAnimation(Popup.HorizontalOffsetProperty, animX);
				DragIcon.BeginAnimation(Popup.VerticalOffsetProperty, animY);

			}
		}

		private void AnimX_Completed(object sender, EventArgs e)
		{
			Dispose();
		}
	}


}
