/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-10-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPFhelper
{

	/// <summary>
	/// Provide a way to depict an UIElement area and an optional associated zone
	/// </summary>
	public class DebugAdorner : Adorner
	{
		/// <summary>
		/// Color of the decoration
		/// </summary>
		public Color Color;

		/// <summary>
		/// Zone of interest associated to this object (null: object itself)
		/// </summary>
		public Rect? Rect = null;

		/// <summary>
		/// Reference of the rectangle
		/// </summary>
		public UIElement Reference = null;
		
		/// <summary>
		/// Message to be displayed on the adorned element
		/// </summary>
		public string Text = null;


		// Handle the DebugAdorner resources
		private static Dictionary<UIElement, WeakReference> DebugAdornerList = null; 
		private static Random Random = new Random();
		private AdornerLayer AdornerLayer;

		/// <summary>
		/// Constructor can be only called through class functions
		/// </summary>
		/// <param name="adornedElement">element to be decorated</param>
		/// <param name="color">Color of the decoration</param>
		/// <param name="rect">Zone of interest associated to this object (null: object itself)</param>
		private DebugAdorner(UIElement adornedElement, Color color, Rect? rect, UIElement reference, string text) : base(adornedElement)
		{
			Color = color;
			Rect = rect;
			Reference = null;
			Text = text;
			AllowDrop = false;
			IsHitTestVisible = false;
			AdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
			AdornerLayer.Add(this);
		}


		/// <summary>
		/// Destructor
		/// </summary>
		public void Destroy()
		{
			AdornerLayer.Remove(this);
		}


		// A common way to implement an adorner's rendering behavior is to override the OnRender
		// method, which is called by the layout system as part of a rendering pass.
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (AdornedElement.Visibility == Visibility.Collapsed)
			{
				return;
			}

			// Handle the colors
			Pen renderPen = new Pen(new SolidColorBrush(Color), 0.5);
			Pen linePen = new Pen(new SolidColorBrush(Color), 0.2);

			// Get real Adoner element rectangle and readjust its position (margins mess this up)
			var off = VisualTreeHelper.GetOffset(AdornedElement); // Readjust origin
			Rect layoutZone = new Rect(new Point(-off.X, -off.Y), AdornedElement.DesiredSize);

			// Get the zone of interest
			var zoneOfInterest = Rect != null ? Rect.Value : layoutZone;

			// Decorate the layout element with squared points
			drawingContext.DrawRectangle(null, renderPen, RectanglePoint(layoutZone.TopLeft));
			drawingContext.DrawRectangle(null, renderPen, RectanglePoint(layoutZone.TopRight));
			drawingContext.DrawRectangle(null, renderPen, RectanglePoint(layoutZone.BottomLeft));
			drawingContext.DrawRectangle(null, renderPen, RectanglePoint(layoutZone.BottomRight));

			// Decorate the rendered zone with dots
			if (AdornedElement is FrameworkElement elm)
			{
				Rect renderZone = new Rect(0, 0, elm.ActualWidth, elm.ActualHeight);
				if (Rect == null) zoneOfInterest = renderZone;

				const double renderRadius = 5.0;
				drawingContext.DrawEllipse(null, renderPen, renderZone.TopLeft, renderRadius, renderRadius);
				drawingContext.DrawEllipse(null, renderPen, renderZone.TopRight, renderRadius, renderRadius);
				drawingContext.DrawEllipse(null, renderPen, renderZone.BottomLeft, renderRadius, renderRadius);
				drawingContext.DrawEllipse(null, renderPen, renderZone.BottomRight, renderRadius, renderRadius);

				// Draw bounds from layout zone to the rendered zone
				drawingContext.DrawLine(linePen, layoutZone.TopLeft, renderZone.TopLeft);
				drawingContext.DrawLine(linePen, layoutZone.TopRight, renderZone.TopRight);
				drawingContext.DrawLine(linePen, layoutZone.BottomLeft, renderZone.BottomLeft);
				drawingContext.DrawLine(linePen, layoutZone.BottomRight, renderZone.BottomRight);
			}

			// Decorate the zone of interest with a square
			if (Rect != null && Reference != null)
			{
				var trans = Reference.TransformToDescendant(AdornedElement);
				if (trans != null)
				{
					zoneOfInterest = new Rect(
						trans.Transform(zoneOfInterest.TopLeft), 
						trans.Transform(zoneOfInterest.BottomRight)
						);
				}
			}


			drawingContext.DrawRectangle(null, renderPen, zoneOfInterest);

			// Draw bounds from layout zone to its zone of interest
			if (Rect != null)
			{
				drawingContext.DrawLine(linePen, layoutZone.TopLeft, zoneOfInterest.TopLeft);
				drawingContext.DrawLine(linePen, layoutZone.TopRight, zoneOfInterest.TopRight);
				drawingContext.DrawLine(linePen, layoutZone.BottomLeft, zoneOfInterest.BottomLeft);
				drawingContext.DrawLine(linePen, layoutZone.BottomRight, zoneOfInterest.BottomRight);
			}


			// Display text
			if (Text != null)
			{
				var format = new FormattedText(
					Text,
					System.Globalization.CultureInfo.InvariantCulture,
					FlowDirection.LeftToRight,
					new Typeface("Arial"), 12,
					new SolidColorBrush(Color),
					1.0);

				drawingContext.DrawText(format, layoutZone.TopLeft);
			}
		}

		private Rect RectanglePoint(Point point, double renderRadius = 5.0)
		{
			return new Rect(point.X - renderRadius, point.Y - renderRadius, renderRadius * 2, renderRadius * 2);
		}


		protected override Size MeasureOverride(Size constraint)
		{
			var result = base.MeasureOverride(constraint);
			InvalidateVisual();
			return result;
		}


		//      +--------------------------------------+
		//      | Arbitrary zone                       |
		//		|                                      |
		//      |    □ . . . . . . . . . . . . . . □   |
		//      |    . Layout zone                 .   |
		//      |    .                             .   |
		//      |    .          O . . . . . .O     .   |
		//      |    .          . Rendered   .     .   |
		//      |    .          .    zone    .     .   |
		//      |    .<-Margin->O . . . . . .O     .   |
		//		|    .                             .   |
		//      |    □ . . . . . . . . . . . . . . □   |
		//      +--------------------------------------+
		/// <summary>
		/// Create/show decoration on an UIElement to help on its debugging.
		/// Layout Zone (including magin of the element) corners are depicted with squares, 
		/// Rendered Zone (element visual itself) corners is depicted with dots, and
		/// an arbitrary zone can be added shown as a square (default: layout zone)
		/// </summary>
		/// <param name="elm">UIElement to be decorated</param>
		/// <param name="rect">Zone of interest associated to this object, relative to the element (null: rendered zone)</param>
		/// <param name="reference">UI reference for the rect coordinates (default: elm is the reference)</param>
		/// <param name="color">Color of the decoration (default: random color)</param>
		/// <param name="text">message attached displayed on the element (default: no message)</param>
		[Conditional("DEBUG")]
		public static void Show(UIElement elm, Rect? rect = null, UIElement reference = null, Color? color = null, string text = null)
		{
			if (elm == null) return;

			if (DebugAdornerList == null)
			{
				DebugAdornerList = new Dictionary<UIElement, WeakReference>();
			}
			else
			{
				// Garbage collection
				var keys = DebugAdornerList.Keys.ToArray();
				for (int i = DebugAdornerList.Keys.Count - 1; i >= 0; i--)
				{
					var key = keys[i];
					if (DebugAdornerList.TryGetValue(key, out WeakReference rref))
					{
						if (!rref.IsAlive)
						{
							DebugAdornerList.Remove(key);
						}
					}
				}
			}


			DebugAdorner adorner = null;
			if (DebugAdornerList.TryGetValue(elm, out WeakReference aref))
			{
				if (aref.IsAlive && aref.Target is DebugAdorner ad)
				{
					adorner = ad;
				}
			}

			if (adorner == null)
			{

				// default
				Color col;
				if (color != null)
				{
					col = color.Value;
				}
				else
				{
					// Pick random color
					var num = new byte[2];
					Random.NextBytes(num);
					col = new Color()
					{
						A = 255,
						R = 255,
						G = num[0],
						B = num[1]
					};
				}

				try
				{
					adorner = new DebugAdorner(elm, col, rect, reference, text);
					DebugAdornerList.Add(elm, new WeakReference(adorner));
				}
				catch { }
			}
			else
			{
				if (color != null) adorner.Color = color.Value;
				if (rect != null) adorner.Rect = rect;
				if (reference != null) adorner.Reference = reference;
				if (text != null) adorner.Text = text;
				adorner.InvalidateVisual();
			}
		}


		/// <summary>
		/// Create/show decoration on an UIElement with a message attached to it.
		/// </summary>
		/// <param name="elm">UIElement to be decorated</param>
		/// <param name="text">message attached displayed on the element</param>
		/// <param name="color">Color of the decoration</param>
		[Conditional("DEBUG")]
		public static void Show(UIElement elm, string text, Color color)
		{
			Show(elm, null, null, color, text);
		}




		/// <summary>
		/// Destroy the decoration on one or all elements
		/// </summary>
		/// <param name="elm">element to free from its decoration (null: remove all decorations)</param>
		[Conditional("DEBUG")]
		public static void Hide(UIElement elm = null)
		{
			if (DebugAdornerList == null) return;

			// Garbage collection and adorner destruction
			var keys = DebugAdornerList.Keys.ToArray();
			for (int i = DebugAdornerList.Keys.Count - 1; i>=0 ; i-- )
			{
				var key = keys[i];
				if (DebugAdornerList.TryGetValue(key, out WeakReference rref))
				{
					if (rref.IsAlive)
					{
						if (elm == null || key == elm)
						{
							if (rref.Target is DebugAdorner adorner)
							{
								adorner.Destroy();
								DebugAdornerList.Remove(key);
							}
						}
					}
					else
					{
						DebugAdornerList.Remove(key);
					}
				}
			}
		}


	}
}
