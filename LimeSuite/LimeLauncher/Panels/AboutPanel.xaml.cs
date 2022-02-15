/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     01-10-2015
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				// Open file with default program in .NET core +
				using var proc = new Process();
				proc.StartInfo.FileName = "explorer";
				proc.StartInfo.Arguments = $"\"{e.Uri.AbsoluteUri}\"";
				proc.Start();
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
		public static List<About.Credit> Credits { get; private set; }

        /// <summary>
        /// Credit Definition
        /// </summary>
        private static readonly List<About.Credit> LimeLauncherCredits = new()
        {
            new About.Credit{ Item="DWM Thumbnails WPF", Author="Douglas Stockwell", URL="http://www.11011.net/archives/000653.html" },
            new About.Credit{ Item="NotifyIconWPF", Author="Philipp Sumi", URL="http://www.hardcodet.net/wpf-notifyicon" },
            new About.Credit{ Item="ColorPicker", Author="Ury Jamshy", URL="https://www.codeproject.com/Articles/229442/WPF-Color-Picker-VS-Style" },
            new About.Credit{ Item="UniversalValueConverter WPF", Author="Colin Eberhardt", URL="http://blog.scottlogic.com/2010/07/09/a-universal-value-converter-for-wpf.html" },
			new About.Credit{ Item="Icons (inconmontr)", Author="Alexander Kahlkopf", URL="https://iconmonstr.com/" },
		};

		#endregion


	}
}
