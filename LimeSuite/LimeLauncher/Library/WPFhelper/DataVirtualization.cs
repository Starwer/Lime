/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     11-03-2018
* Copyright :   Sebastien Mouy © 2019  
**************************************************************************/

using Lime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace WPFhelper
{
	/// <summary>
	/// Enable to do Data-Virtualization on any Control, without the need of UWP
	/// </summary>
	public class DataVirtualization
	{
		/// <summary>
		/// Delay update of Virtulization using this timer
		/// </summary>
		private static DispatcherTimer Timer = null;

		/// <summary>
		/// Track the association of the scrollviewers associated with ItemsControls
		/// </summary>
		private static Dictionary<ScrollViewer, List<WeakReference>> References = null;


		// ----------------------------------------------------------------------------------------------
		#region Attached Properties

		/// <summary>
		/// Target for the Data-Virtualization on an ItemsControl
		/// </summary>
		public static readonly DependencyProperty TargetProperty =
		DependencyProperty.RegisterAttached("Target", typeof(object),
			typeof(DataVirtualization), new PropertyMetadata(null, TargetChanged));

		public static object GetTarget(ItemsControl obj)
		{
			return obj.GetValue(TargetProperty);
		}

		public static void SetTarget(ItemsControl obj, object value)
		{
			obj.SetValue(TargetProperty, value);
		}


		/// <summary>
		/// Gets or sets the delay before the virtualization takes effects
		/// </summary>
		public static readonly DependencyProperty DelayProperty =
		DependencyProperty.RegisterAttached("Delay", typeof(TimeSpan),
			typeof(DataVirtualization),
			new FrameworkPropertyMetadata(
				TimeSpan.FromSeconds(0.1),
				FrameworkPropertyMetadataOptions.Inherits));

		public static TimeSpan GetDelay(UIElement obj)
		{
			return (TimeSpan)obj.GetValue(DelayProperty);
		}

		public static void SetDisable(UIElement obj, TimeSpan value)
		{
			obj.SetValue(DelayProperty, value);
		}



		#endregion


		// ----------------------------------------------------------------------------------------------
		#region Class functions


		private static void TargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxthis = (ItemsControl)d;

			if (e.OldValue is IItemsRangeInfo rinfo)
			{
				rinfo.Close();
			}

			var enable = e.NewValue is IItemsRangeInfo || e.NewValue is ISelectionInfo;

			if (enable)
			{
				// Lazy declaration of Timer
				if (Timer == null)
				{
					Timer = new DispatcherTimer();
					Timer.Tick += OnTimerElapsed;
				}

				wxthis.Loaded += OnLoaded;
				wxthis.Unloaded += OnUnloaded;
			}
			else
			{
				wxthis.Loaded -= OnLoaded;
				wxthis.Unloaded -= OnUnloaded;
			}

			ScrollViewer wxscroll;
			if (wxthis is ListBox)
			{
				wxscroll = WPF.FindFirstChild<ScrollViewer>(wxthis);
			}
			else
			{
				wxscroll = WPF.FindFirstParent<ScrollViewer>(wxthis);
			}

			if (References == null)
			{
				References = new Dictionary<ScrollViewer, List<WeakReference>>();
			}

			if (References.TryGetValue(wxscroll, out var reflist))
			{
				// Remove dead references
				for (int i = 0; i < reflist.Count; i++)
				{
					var wref = reflist[i];
					if (!wref.IsAlive || wref.Target == wxthis)
					{
						reflist.RemoveAt(i);
						i--;
					}
				}
			}
			else if (enable)
			{
				reflist = new List<WeakReference>();
				References.Add(wxscroll, reflist);
			}

			if (enable)
			{
				reflist.Add(new WeakReference(wxthis));
				if (reflist.Count == 1)
				{
					wxscroll.ScrollChanged += OnScrollChanged;
				}
			}
			else if (reflist == null || reflist.Count == 0)
			{
				wxscroll.ScrollChanged -= OnScrollChanged;
			}

		}


		/// <summary>
		/// Track unloaded ItemsControls
		/// </summary>
		/// <param name="sender">ItemsControl</param>
		/// <param name="e"></param>
		private static void OnUnloaded(object sender, RoutedEventArgs e)
		{
			LimeMsg.Debug("DataVirtualization OnUnloaded");
			if (sender == null) return;
			var wxthis = (ItemsControl)sender;

			if (GetTarget(wxthis) is IItemsRangeInfo rinfo)
			{
				rinfo.Close();
			}
		}

		/// <summary>
		/// Track loaded ItemsControls
		/// </summary>
		/// <param name="sender">ItemsControl</param>
		/// <param name="e"></param>
		private static void OnLoaded(object sender, RoutedEventArgs e)
		{
			LimeMsg.Debug("DataVirtualization OnLoaded");
			if (sender == null) return;
			var wxthis = (ItemsControl)sender;
		}


		/// <summary>
		/// Schedule the virtualization update to avoid too expensive CPU access during UI operation
		/// </summary>
		/// <param name="sender">ScrollViewer</param>
		/// <param name="e"></param>
		private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			LimeMsg.Debug("DataVirtualization OnScrollChanged");
			var wxthis = (UIElement)sender;

			Timer.Stop();

			// Complete previous operation
			if (Timer.Tag != null && wxthis!= Timer.Tag)
			{
				OnTimerElapsed();
			}

			// Schedule new Timer operation
			var delay = GetDelay(wxthis);
			Timer.Tag = wxthis;
			Timer.Interval = delay;

			Timer.Start();
		}

		/// <summary>
		/// Update the virtualization of the scrollviewer in <see cref="Timer.Tag"/>
		/// </summary>
		/// <param name="sender"><see cref="Timer"/></param>
		/// <param name="e"></param>
		private static void OnTimerElapsed(object sender = null, EventArgs e = null)
		{
			LimeMsg.Debug("DataVirtualization OnTimerElapsed");

			Timer.Stop();
			var wxscroll = (ScrollViewer)Timer.Tag;
			Timer.Tag = null;

			var reflist = References[wxscroll];

			foreach (var wref in reflist)
			{
				if (!wref.IsAlive || !(wref.Target is ItemsControl wxctrl)) continue;

				var itemgen = wxctrl.ItemContainerGenerator;
				var itemcnt = itemgen.Items.Count;

				int startSelected = -1;
				int endSelected = -1;
				var rangeSelected = new List<ItemIndexRange>();

				int startVisible = -1;
				int endVisible = -1;
				var rangeVisible = new List<ItemIndexRange>();

				for (int i = 0; i < itemcnt; i++)
				{
					if (!(itemgen.ContainerFromIndex(i) is FrameworkElement wxitem)) continue;
					if (wxitem.DataContext is IItemsRangeInfo)
					{
						// Detect selected items
						if (wxitem.IsFocused)
						{
							if (startSelected == -1) startSelected = i;
							endSelected = i;
						}
						else if (Selector.GetIsSelected(wxitem))
						{
							if (startSelected == -1) startSelected = i;
							endSelected = i;
						}
						else if(startSelected != -1)
						{
							rangeSelected.Add(new ItemIndexRange(startSelected, (uint)(endSelected - startSelected + 1)));
							startSelected = -1;
						}

						// Detect visible items
						if (WPF.IsFullyOrPartiallyVisible(wxitem, wxscroll))
						{
							if (startVisible == -1) startVisible = i;
							endVisible = i;
						}
						else if (startVisible != -1)
						{
							rangeVisible.Add(new ItemIndexRange(startVisible, (uint)(endVisible - startVisible + 1)));
							startVisible = -1;
						}

					}
				}

				var target = GetTarget(wxctrl);

				// TODO: handle selection


				if (target is IItemsRangeInfo rinfo)
				{
					if (startVisible != -1)
					{
						rangeVisible.Add(new ItemIndexRange(startVisible, (uint)(endVisible - startVisible + 1)));
						startVisible = -1;
					}

					if (rangeVisible.Count > 0)
					{
						rinfo.RangesChanged(rangeVisible[0], new ReadOnlyCollection<ItemIndexRange>(rangeVisible));
					}
				}
			}
		}

		#endregion

	}

}
