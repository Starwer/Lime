/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     01-10-2015
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lime;

namespace LimeLauncher
{
	/// <summary>
	/// Interaction logic for sysAbout.xaml
	/// </summary>
	[SupportedOSPlatform("windows")]
	public partial class AboutPanel : UserControl
    {
        // --------------------------------------------------------------------------------------------------
        #region Basic Control logic

        public AboutPanel()
        {
			// Populate credits data
            if (Credits == null) {
                Credits = new List<About.Credit>(About.Credits);
                Credits.AddRange(LimeLauncherCredits);
            }

            InitializeComponent();
        }


        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (oldContent != null)
                throw new InvalidOperationException("Content can't be set!");
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


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Credits definition

		/// <summary>
		/// Represent the credits for the application
		/// </summary>
		public static List<About.Credit> Credits = null;

        /// <summary>
        /// Credit Definition
        /// </summary>
        private static readonly List<About.Credit> LimeLauncherCredits = new List<About.Credit>()
        {
            new About.Credit{ item="DWM Thumbnails WPF", author="Douglas Stockwell", url="http://www.11011.net/archives/000653.html" },
            new About.Credit{ item="NotifyIconWPF", author="Philipp Sumi", url="http://www.hardcodet.net/wpf-notifyicon" },
            new About.Credit{ item="ColorPicker", author="Ury Jamshy", url="https://www.codeproject.com/Articles/229442/WPF-Color-Picker-VS-Style" },
            new About.Credit{ item="UniversalValueConverter WPF", author="Colin Eberhardt", url="http://blog.scottlogic.com/2010/07/09/a-universal-value-converter-for-wpf.html" },
			new About.Credit{ item="Icons (inconmontr)", author="Alexander Kahlkopf", url="https://iconmonstr.com/" },
		};

		#endregion


	}
}
