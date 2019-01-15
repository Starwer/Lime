/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     17-03-2018
* Copyright :   Sebastien Mouy © 2018 
**************************************************************************/


using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LimeLauncher
{
    /// <summary>
    /// Interaction logic for SkinPanel.xaml
    /// </summary>
    public partial class SkinPanel : UserControl
    {
        public SkinPanel()
        {
            InitializeComponent();
        }


        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            wxSkinInfoText.TextWrapping = TextWrapping.WrapWithOverflow;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            wxSkinInfoText.TextWrapping = TextWrapping.NoWrap;
        }

		/// <summary>
		/// Enable to open Hyperlink in external browser
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			try
			{
				var url = new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri);
				System.Diagnostics.Process.Start(url);
			}
			catch { }

			e.Handled = true;
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


		/// <summary>
		/// Provide a generic callback to PreviewKeyDown event to fix navigation problems using arrow keys
		/// </summary>
		/// <param name="sender">Any FrameworkElement</param>
		/// <param name="e"></param>
		private void Expander_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			var wxobj = e.OriginalSource as Framework​Element;
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

	}
}
