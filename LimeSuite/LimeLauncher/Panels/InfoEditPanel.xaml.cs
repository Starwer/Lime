/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     13-09-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using Lime;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using WPFhelper;

namespace LimeLauncher
{
	/// <summary>
	/// Interaction logic for InfoEditPanel.xaml
	/// </summary>
	public partial class InfoEditPanel : UserControl
	{
		// --------------------------------------------------------------------------------------------------
		#region Basic Control logic

		public InfoEditPanel()
		{
			InitializeComponent();

			// Manual binding to enable the detection of binding change
			this.SetBinding(BoundDataContextProperty, new Binding());

		}


		protected override void OnContentChanged(object oldContent, object newContent)
		{
			if (oldContent != null)
				throw new InvalidOperationException("Content can't be set!");
		}

		public static readonly DependencyProperty BoundDataContextProperty = DependencyProperty.Register(
			"BoundDataContext", typeof(object), typeof(InfoEditPanel), new PropertyMetadata(null, OnBoundDataContextChanged)
			);



		/// <summary>
		/// React on Binding change
		/// </summary>
		/// <param name="d"></param>
		/// <param name="e"></param>
		private static void OnBoundDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var wxThis = d as InfoEditPanel;
			if (e.NewValue is LimeItem item)
			{
				LimeMsg.Debug("InfoEditPanel: BoundDataContext: {0}", item.Name);
				LimeLib.LifeCheck();

				var meta = item.Metadata;
				if (meta != null && meta.Search == null)
				{
					if (meta.Type == MediaType.Video)
					{
						var path = meta["Path"].Value;
						var repl = Global.User.PathToSearchMovie.Replace(path);
						meta.Search = new LimeProperty("Search", repl);
					}
				}

			}
#if TRACE
			wxThis.StopWatch.Restart();
#endif
		}


#if TRACE
		protected Stopwatch StopWatch = new Stopwatch();
#endif



		/// <summary>
		/// Track changes in the Bound LimeItem
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void Metadata_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var item = (LimeMetadata)sender;
			if (e != null && e.PropertyName == "Item" && item == _Metadata && 
				e is LimePropertyChangedEventArgs ev)
			{
				var key = ev.ItemPath;
				if (key == "Actors" || key == "Artists" || key == "Director" || key == "Conductor")
				{
					item.LoadAsync((int)Global.User.PersonAutoDownload);
				}
			}
		}


		public bool Big
		{
			get { return (bool)this.GetValue(BigProperty); }
			private set { this.SetValue(BigProperty, value); }
		}
		public static readonly DependencyProperty BigProperty = DependencyProperty.RegisterAttached(
			"Big", typeof(bool), typeof(InfoEditPanel), new PropertyMetadata()
			);


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region GUI Callbacks

		/// <summary>
		/// Handle the Async Loading
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScrollViewer_LayoutUpdated(object sender, EventArgs e)
		{
			if (wxPropCtrl.DataContext is LimeMetadata meta && meta != _Metadata)
			{
				LimePerson.Cancel();

				_Metadata = meta;
				_Metadata.PropertyChangedWeak += Metadata_PropertyChanged;

#if TRACE
				StopWatch.Stop();
				var time = StopWatch.Elapsed;
				LimeMsg.Info("TraceStopWatch", time);
#endif
				// Force to start the Loading of person metadata asynchronously and after the main page has been rendered
				meta.LoadAsync((int)Global.User.PersonAutoDownload);

			}
		}
		private LimeMetadata _Metadata = null;


		private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var wxobj = sender as ScrollViewer;
			double refsize = (Global.Local.Skin.IconBigSize.Content as DoubleScaled).Scaled;
			Big = wxobj.ActualWidth > refsize * 2.5;
			var bigger = wxobj.ActualWidth > refsize * 6;

			if (bigger)
			{
				Grid.SetRow(wxPersons, 0);
				Grid.SetColumn(wxPersons, 1);
				wxCol1.Width = new GridLength(1, GridUnitType.Star);
			}
			else
			{
				Grid.SetRow(wxPersons, 1);
				Grid.SetColumn(wxPersons, 0);
				wxCol1.Width = new GridLength(0);
			}

			if (!Big)
			{
				wxInfoEditPanelGridHighRow.MaxHeight = wxInfoEditPanelGrid.ActualHeight / 4.0;
				wxColumnIcon.MinWidth = 0;
			}
			else
			{
				wxInfoEditPanelGridHighRow.MaxHeight = wxInfoEditPanelGrid.ActualHeight - refsize;
				wxColumnIcon.MinWidth = refsize;
			}
		}



		/// <summary>
		/// Handle Tab
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void wxInfoEditPanelGrid_KeyDown(object sender, KeyEventArgs e)
		{
			//LimeMsg.Debug("wxInfoEditPanelGrid_KeyDown");

			if (e.Key == Key.Tab)
			{
				e.Handled = true;
				var direction = Keyboard.Modifiers == ModifierKeys.Shift ? FocusNavigationDirection.Left : FocusNavigationDirection.Right;
				TraversalRequest tRequest = new TraversalRequest(direction);
				var wxobj = Keyboard.FocusedElement as UIElement;
				wxobj.MoveFocus(tRequest);
			}
		}

		/// <summary>
		/// Set default values for Grid splitters
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void wxInfoEditPanelGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//LimeMsg.Debug("wxInfoEditPanelGrid_SizeChanged: {0} {1}", wxInfoEditPanelBorder.ActualWidth, wxInfoEditPanelBorder.ActualHeight);
			if (Global.Local.PicturePaneSize < -0.5)
				Global.Local.PicturePaneSize = wxInfoEditPanelBorder.ActualHeight / 4;

			if (Global.Local.MetadataSearchPaneSize < -0.5 && wxInfoEditPanelBorder.ActualWidth > 0)
				Global.Local.MetadataSearchPaneSize = wxInfoEditPanelBorder.ActualWidth / 2;
		}



        /// <summary>
        /// Handle the busy state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxInfoEditPanelBorder_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var busy = ! (bool) e.NewValue;
            LimeMsg.Debug("wxInfoEditPanelBorder_IsEnabledChanged: {0}", busy);

            this.Cursor = busy ? Cursors.Wait : null;
            wxInfoEditWait.IsEnabled = busy;
        }



		private void Person_Click(object sender, RoutedEventArgs e)
		{
			var wxpers = (Button)e.OriginalSource;
			var person = (LimePerson)wxpers.DataContext;
			if (person != null)
			{
				person.LoadAsync(true);
			}
		}


		#endregion

	}

}
