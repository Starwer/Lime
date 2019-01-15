/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2018 
**************************************************************************/

using Lime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LimeLauncher
{
    /// <summary>
    /// Interaction logic for ConfigPanel.xaml
    /// </summary>
    public partial class ConfigPanel : UserControl
    {

        // --------------------------------------------------------------------------------------------------
        #region Static  properties
        
        /// <summary>
        /// Keep the selected Pane persistent
        /// </summary>
        private static int SelectedItem = 0;

        /// <summary>
        /// Refocus at startup
        /// </summary>
        private bool Refocus = true;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        public ConfigPanel()
        {
            InitializeComponent();

            // Try to avoid troubles in the designer view
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // Command Binding registration
            Commands.Bind(this, Global.ConfigTree.ConfigurationCommands);

            // Initialize object
            Help.Refresh();

            // Restore selected panel
            wxPanelList.SelectedIndex = SelectedItem;

        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region GUI callbacks

        /// <summary>
        /// Handle the Panel Selection by the ListBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wxPanelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prepare the scroll
            var scroll = wxScroll.VerticalOffset;
            wxScroll.ScrollToVerticalOffset(0);

            var selection = e.AddedItems?[0] as FrameworkElement;
            int idx = 0;
            foreach (FrameworkElement item in wxPanels.Items)
            {
                // Store Scroll position
                if (item.Visibility == Visibility.Visible)
                {
                    item.Tag = scroll;
                }

                if (item == selection)
                {
                    // Set visibility
                    item.Visibility = Visibility.Visible;

                    // Restore Scroll position
                    if (item.Tag is double scr) wxScroll.ScrollToVerticalOffset(scr);

                    // Store selected
                    SelectedItem = idx;
                }
                else
                {
                    item.Visibility = Visibility.Collapsed;
                }

                idx++;
            }

        }


        private void wxPanelList_LayoutUpdated(object sender, System.EventArgs e)
        {
            if (Refocus)
            {
				if (wxPanelList.ItemContainerGenerator.ContainerFromItem(wxPanelList.SelectedItem) is ListBoxItem wxitem)
					Keyboard.Focus(wxitem);
				Refocus = false;
            }

        }


        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
			if (!(sender is FrameworkElement wxobj)) return;
			if (Keyboard.Modifiers != 0) return;

            TraversalRequest tRequest = null;
			switch (e.Key)
			{
				case Key.Left: tRequest = new TraversalRequest(FocusNavigationDirection.Left); break;
				case Key.Right: tRequest = new TraversalRequest(FocusNavigationDirection.Right); break;
				case Key.Down:
					if (wxPanelList.Items != null && wxPanelList.SelectedIndex == wxPanelList.Items.Count - 1)
					{
						tRequest = new TraversalRequest(FocusNavigationDirection.Down); 
					}
					break;
			}

            if (tRequest != null)
            {
                wxobj.MoveFocus(tRequest);
                e.Handled = true;
            }
        }


		#endregion
	}
}
