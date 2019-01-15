/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     18-01-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using System;
using System.Windows.Controls;
using System.Windows.Documents;
using Lime;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using LimeLauncher.Controls;
using System.Windows.Input;

namespace LimeLauncher
{
    /// <summary>
    /// Interaction logic for HelpPanel.xaml
    /// </summary>
    public partial class HelpPanel : UserControl
    {
        // --------------------------------------------------------------------------------------------------
        #region Basic Control logic

        public HelpPanel()
        {
            InitializeComponent();
            Refresh();
        }


        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (oldContent != null)
                throw new InvalidOperationException("Content can't be set!");
        }


        /// <summary>
        /// Refresh the content of the Help
        /// </summary>
        public void Refresh()
        {
            var items = new List<LimeProperty>();

            foreach (var arg in Global.Properties)
            {
                if (arg.Visible) items.Add(arg);
            }

            foreach (var arg in Global.Local.Skin.Parameters)
            {
                if (arg.Visible && arg.Content != null) items.Add(arg);
            }

            wxCmdTable.ItemsSource = items;
        }

		/// <summary>
		/// Enable to open Hyperlink in external browser
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			e.Handled = true;

			var wxObj = e.OriginalSource as Hyperlink;

			string opt; 
			if (wxObj.DataContext is LimeProperty prop)
			{
				opt = LimeProperty2CliConverter.Get(prop);
			}
			else
			{
				opt = wxObj.DataContext as string;
			}

			if (opt == null) return;
			opt += " ";
			LimeMsg.Info("CopyClip", opt);
			Clipboard.SetText(opt);
		}


		/// <summary>
		/// Provide a generic callback to PreviewKeyDown event to fix navigation problems using arrow keys
		/// </summary>
		/// <param name="sender">Any FrameworkElement</param>
		/// <param name="e"></param>
		private void FixFocus_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			var wxobj = e.OriginalSource as Framework​Content​Element;
			if (wxobj == null) return;
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


		private void LimeLabel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var width = (sender as LimeLabel).ActualWidth;
			width = (width - 20) / 3.0;
			wxColHeader.MaxWidth = width <= 20 ? 20 : width; 

		}

		#endregion

	}
}
