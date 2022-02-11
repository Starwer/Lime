/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Interop;
using WPFhelper;

namespace LimeLauncher
{
    /// <summary>
    /// Lime Launcher Configuration Panel
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public partial class Configuration : Window
    {
        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// Keep the handle reference of this window
        /// </summary>
        public IntPtr Handle { get; private set; }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        public Configuration()
        {
            InitializeComponent();

            // Try to avoid troubles in the designer view
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // Avoid the main-window to come over
            Commands.MainWindow.Topmost = false;

            // Override the application resources if not already overridden in Skin
            var keys = Resources.MergedDictionaries[0].Keys;
            foreach (var key in Application.Current.Resources.Keys)
            {
                if (key is Type) // Only types (implicit styles) require this trick
                {
                    var wxobj = Application.Current.Resources[key];
                    if (wxobj is Style st)
                    {
                        bool found = false;
                        foreach (var skey in keys)
                        {
                            if (found = key == skey) break;
                        }
                        if (!found)
                        {
                            // Create empty style to shadow the application Style
                            Resources.Add(key, new Style(st.TargetType));
                        }
                    }
                }
            }


            // Apply Style fully
            Style = (Style)FindResource(typeof(Window));
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region GUI Callbacks

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Restore window state/dimensions
            Handle = ((HwndSource)PresentationSource.FromVisual(this)).Handle;
            var wcfg = Global.Local.WindowSettings;
            if (wcfg.Length > 0) WPF.SetWindowPlacement(Handle, wcfg, this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save window state/dimensions
            var handle = ((HwndSource)PresentationSource.FromVisual(this)).Handle;
            Global.Local.WindowSettings = WPF.GetWindowPlacement(handle);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Commands.MainWindow.Topmost = Global.User.TopMost;
            Commands.ConfigClose.Execute();
        }

        #endregion

    }
}
