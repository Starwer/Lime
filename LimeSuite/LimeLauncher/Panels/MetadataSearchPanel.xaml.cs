/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     01-01-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/

using Lime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using WPFhelper;

namespace LimeLauncher
{
	/// <summary>
	/// Interaction logic for MetadataSearchPanel.xaml
	/// </summary>
	public partial class MetadataSearchPanel : UserControl
	{

		// --------------------------------------------------------------------------------------------------
		#region Dependency properties


		private const string LanguageSection = "Translator";


		/// <summary>
		/// List of the found items
		/// </summary>
		public List<LimeOpus> Items
		{
			get { return (List<LimeOpus>)this.GetValue(ItemsProperty); }
			private set { this.SetValue(ItemsProperty, value); }
		}
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.RegisterAttached(
			"Items", typeof(List<LimeOpus>), typeof(MetadataSearchPanel), new PropertyMetadata()
			);



		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Basic Control logic

		public MetadataSearchPanel()
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
			"BoundDataContext", typeof(object), typeof(MetadataSearchPanel), new PropertyMetadata(null, OnBoundDataContextChanged)
			);



        /// <summary>
        /// React on Binding change
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnBoundDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wxobj = d as MetadataSearchPanel;

			if (e.NewValue is LimeMetadata meta)
			{
				LimeMsg.Debug("MetadataSearchPanel: BoundDataContext: {0}", meta.Search?.Value);
				if (meta.Search == null) wxobj.Refresh();
			}
		}


        /// <summary>
        /// Refresh the list of found items
        /// </summary>
        public void Refresh()
        {
            LimeMsg.Debug("MetadataSearchPanel: Refresh");
			if (DataContext is LimeMetadata meta && meta.Search != null && Visibility == Visibility.Visible && IsEnabled)
			{
				// Trigger a Search value change event to update the search result
				LimeTextBox_TextValidated(this, new LimeControlEventArgs(this, meta.Search, meta.Search.Value));
			}
			else
			{
				Items = null;
			}
		}


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region GUI Callbacks

        /// <summary>
        /// React on Search-value change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LimeTextBox_TextValidated(object sender, LimeControlEventArgs e)
        {
            var value = e.Value as string;

            if (Visibility == Visibility.Visible && IsEnabled && value != null)
            {
                LimeMsg.Debug("MetadataSearchPanel: LimeTextBox_TextValidated: {0}", value);

                if (string.IsNullOrWhiteSpace(value))
                {
                    Items = null;
                }
                else
                {
                    wxScroll.IsEnabled = false;
                    wxInfoEditWait.IsEnabled = true;

                    // Deselect all buttons
                    for (int i = 0; i < wxPropCtrl.Items.Count; i++)
                    {
                        var node = wxPropCtrl.ItemContainerGenerator.ContainerFromIndex(i);
                        var wxbutton = WPF.FindFirstChild<ToggleButton>(node as FrameworkElement);
                        if (wxbutton != null) wxbutton.IsChecked = false;
                    }

                    try
                    {
                        Items = await Global.Search.SearchVideoAsync(value);
                    }
                    catch
                    {
                        LimeMsg.Error("ErrSearchVideo", value);
                    }

                    wxInfoEditWait.IsEnabled = false;
                    wxScroll.IsEnabled = true;
                }
            }
        }



        /// <summary>
        /// Handle Tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxMetadataSearchPanelGrid_KeyDown(object sender, KeyEventArgs e)
		{
			LimeMsg.Debug("MetadataSearchPanel: wxMetadataSearchPanelGrid_KeyDown");

			if (e.Key == Key.Tab)
			{
				e.Handled = true;
				var direction = Keyboard.Modifiers == ModifierKeys.Shift ? FocusNavigationDirection.Left : FocusNavigationDirection.Right;
				TraversalRequest tRequest = new TraversalRequest(direction);
				var wxobj = Keyboard.FocusedElement as UIElement;
				wxobj?.MoveFocus(tRequest);
			}
		}

		private void wxButton_KeyDown(object sender, KeyEventArgs e)
		{
			LimeMsg.Debug("MetadataSearchPanel: wxButton_KeyDown");

			if (!(sender is FrameworkElement wxobj)) return;
			if (Keyboard.Modifiers != 0) return;

			TraversalRequest tRequest = null;
			switch (e.Key)
			{
				case Key.Up: tRequest = new TraversalRequest(FocusNavigationDirection.Up); break;
				case Key.Down: tRequest = new TraversalRequest(FocusNavigationDirection.Down); break;
				case Key.Left: tRequest = new TraversalRequest(FocusNavigationDirection.Left); break;
				case Key.Right: tRequest = new TraversalRequest(FocusNavigationDirection.Right); break;
			}

			if (tRequest != null)
			{
				wxobj.MoveFocus(tRequest);
				e.Handled = true;
			}
		}


		private async void SearchItem_Click(object sender, RoutedEventArgs e)
		{
			if (!(e.OriginalSource is ToggleButton wxobj)) return;

			if (wxobj.IsChecked == true)
			{
				// Deselect other buttons
				var items = wxPropCtrl.Items;
				for (int i = 0; i < wxPropCtrl.Items.Count; i++)
				{
					var node = wxPropCtrl.ItemContainerGenerator.ContainerFromIndex(i);
					var wxbutton = WPF.FindFirstChild<ToggleButton>(node as FrameworkElement);
					if (wxbutton != null && wxbutton != wxobj)
					{
						wxbutton.IsChecked = false;
					}
				}

				// Select this button
				wxobj.IsChecked = true;
			}

			// Retrieve clicked item data
			var dest = DataContext as LimeMetadata; // Destination (EditInfo pane data)
			var item = wxobj.DataContext as LimeOpus; // selected item (search result)
			var movieid = item.TmdbId;

			LimeMsg.Debug("MetadataSearchPanel: SearchItem_Click: {0}: {1}", item.Title, movieid);

            var wxwait = WPF.FindFirstChild<WaitRotatingDots>(wxobj);
            var wximg = WPF.FindFirstChild<Image>(wxobj);

            LimeMetadata src = null; // Source (Downloaded metadata)
			try
			{
                wximg.Visibility = Visibility.Hidden;
                wxwait.IsEnabled = true;

                src = await Global.Search.GetVideoAsync(movieid);
			}
			catch (Exception ex)
			{
				LimeMsg.Error("ErrMetaDownload", item.Title);
				src = null;
			}
            finally
            {
                wxwait.IsEnabled = false;
                wximg.Visibility = Visibility.Visible;

            }


            // now apply (copy) source metadata to destination metadata
            if (src != null)
			{
				dest.Copy(src);

				// Force Save Button to activate
				CommandManager.InvalidateRequerySuggested();

				// Force to start the Loading of person metadata asynchronously and after the main page has been rendered
				dest.LoadAsync((int)Global.User.PersonAutoDownload);

			}
		}

		private void Button_MouseEnter(object sender, MouseEventArgs e)
		{
			if (!(e.OriginalSource is ToggleButton wxobj)) return;

			if (wxFocused != null && wxFocused != wxobj && wxFocused.IsChecked != true)
			{
				VisualStateManager.GoToState(wxFocused, "Normal", true);
			}

			if (Global.Local.OnTop && Global.Local.CtrlMode == CtrlMode.Mouse)
            {
				VisualStateManager.GoToState(wxobj, "Focused", true);
			}
		}

		private void SearchItem_GotFocus(object sender, RoutedEventArgs e)
		{
			var wxobj = e.OriginalSource as ToggleButton;
			wxFocused = wxobj;
			if (wxobj == null) return;

			// Deselect other buttons
			var items = wxPropCtrl.Items;
			for (int i = 0; i < wxPropCtrl.Items.Count; i++)
			{
				var node = wxPropCtrl.ItemContainerGenerator.ContainerFromIndex(i);
				var wxbutton = WPF.FindFirstChild<ToggleButton>(node as FrameworkElement);
				if (wxbutton != null && wxbutton != wxobj && wxbutton.IsChecked != true)
				{
					VisualStateManager.GoToState(wxbutton, "Normal", true);
				}
			}
		}

		private void SearchItem_LostFocus(object sender, RoutedEventArgs e)
		{
			wxFocused = null;
		}

		private ToggleButton wxFocused = null;

		/// <summary>
		/// Load or unload the Search
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserControl_Update(object sender, DependencyPropertyChangedEventArgs e)
		{
			LimeMsg.Debug("MetadataSearchPanel: UserControl_Update");
			Refresh();
		}

		#endregion

	}

}
