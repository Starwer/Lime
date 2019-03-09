/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     16-04-2016
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System.Windows;
using Lime;
using WPFhelper;
using System;
using System.Windows.Input;
using FolderBrowserTest;
using MSjogren.Samples.ShellLink;
using WindowsInput;
using System.Threading.Tasks;
using System.ComponentModel;

namespace LimeLauncher
{


    /// <summary>
    /// Provides an unified and exhaustive description of all the LimeLauncher commands.
    /// </summary>
    public static class Commands
    {

        // --------------------------------------------------------------------------------------------------
        #region Static Properties

        /// <summary>
        /// Represents the application main window instance.
        /// </summary>
        public static MainWindow MainWindow
        {
            get { return _MainWindow; }
            set
            {
                if (MainWindow != null)
                {
                    throw new Exception("Application can only be set once");
                }
                _MainWindow = value;
            }
        }
        private static MainWindow _MainWindow = null;

        /// <summary>
        /// Represents the Configuration window instance.
        /// </summary>
        public static Configuration CfgWindow { get; set; }


        /// <summary>
        /// Signal that the application is closing (exiting) to avoid re-entrance
        /// </summary>
        public static bool Closing { get; private set; } = false;


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Class functions
        
        /// <summary>
        /// Bind Commands to an UIElement (handle CommandBindings and InputBindings)
        /// </summary>
        /// <param name="wxobj">UIElement to bind</param>
        /// <param name="cmds">Array of LimeCommand to bind</param>
        public static void Bind(UIElement wxobj, LimePropertyCollection cmds)
        {
            foreach (var prop in cmds)
            {
                if (prop != null && prop.Content is LimeCommand cmd && cmd.CommandBinding != null)
                {
                    wxobj.CommandBindings.Add(cmd.CommandBinding);
                }
            }

        }


        /// <summary>
        /// Show the application dialog and return selection
        /// </summary>
        /// <param name="enableDirectory">Enable to create new direcoty.</param>
        /// <returns>selected application, null if cancel/fail</returns>
        private static string AppDialog(bool enableDirectory = true)
        {
			FolderBrowser fb = new FolderBrowser
			{
				Description = Global.Properties["ItemAddApp"].Desc,
				IncludeFiles = true,
				ShowNewFolderButton = enableDirectory
			};

			// Retrieve the Application Folder
			if (LimeLib.IsWindows8)
            {
                ShellShortcut link = new ShellShortcut(ConfigLocal.AppsFolderPath);
                IntPtr pidl = link.PIDL;
                LimeMsg.Debug("AppDialog: AppsFolder PIDL: {0}", pidl.ToInt32());
                fb.RootPIDL = pidl;
                fb.InitialDirectory = String.Format(":{0}", pidl.ToInt32());
            }
            else
            {
                fb.RootFolderID = FolderBrowser.FolderID.StartMenu;
                LimeMsg.Debug("AppDialog: AppsFolder FolderID: {0}", fb.RootFolderID);
            }

            // Show the dialog
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return fb.SelectedPath;
            }

            return null;
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Commands

        // --------------------------------------------------------------------------------------------------
        #region Application commands 

        // -------------------------------------------
        public static LimeCommand Backward = new LimeCommand( NavigationCommands.BrowseBack,
            () => MainWindow.Browser != null && MainWindow.Browser.PanIdx > MainWindow.Browser.PanMin,
            () => {
                MainWindow.Browser.PanIdx--;
            });

        // -------------------------------------------
        public static LimeCommand Forward = new LimeCommand( NavigationCommands.BrowseForward,
            () => MainWindow.Browser != null && MainWindow.Browser.PanIdx < MainWindow.Browser.PanMax,
            () => {
                MainWindow.Browser.PanIdx++;
            });

        // -------------------------------------------
        public static LimeCommand Home = new LimeCommand( NavigationCommands.BrowseHome,
            () => MainWindow.Browser != null && MainWindow.Browser.PanIdx != MainWindow.Browser.PanMin,
            () => {
                MainWindow.Browser.PanIdx = MainWindow.Browser.PanMin;
            });


        // -------------------------------------------
        public static LimeCommand ZoomIn = new LimeCommand(NavigationCommands.IncreaseZoom,
            () => Global.Local.Zoom < Global.Properties["Scale"].Maximum,
            () => {
                Global.Local.Zoom += (Global.Local.Zoom >= 1.10) ? 0.02 : 0.01;
            });


        // -------------------------------------------
        public static LimeCommand ZoomOut = new LimeCommand(NavigationCommands.DecreaseZoom,
            () =>  Global.Local.Zoom > Global.Properties["Scale"].Minimum,
            () => {
                Global.Local.Zoom -= (Global.Local.Zoom >= 1.10) ? 0.02 : 0.01;
            });


        // -------------------------------------------
        public static LimeCommand ConfigSave = new LimeCommand(
            () => Global.User.Modified || Global.Local.Modified,
            () => {
                bool ok = true;
                ok = ok && Global.User.Save();
                ok = ok && Global.Local.Save();
                if (ok)
                {
                    Global.Properties.ReqAdmin = false;
					Global.Local.ConfigVisible = false;
				}
            },
			toggle: null, icon: "Save");

        // -------------------------------------------
        public static LimeCommand SkinRestoreDefault = new LimeCommand(
            () =>  MainWindow != null,
            () => {
                MainWindow.LoadSkin(null, false);
            });

        // -------------------------------------------
        public static LimeCommand SkinReload = new LimeCommand(
			() => MainWindow != null,
			() => {
				if (MainWindow.LoadSkin(null, true))
				{
					LimeMsg.Info("LoadedSkin", Global.User.Skin);
				}
			});

        // -------------------------------------------
        public static LimeCommand OpenUrl = new LimeCommand(
            null,
            () => {
                System.Diagnostics.Process.Start(About.url);
            });

        // -------------------------------------------
        public static LimeCommand Refresh = new LimeCommand( NavigationCommands.Refresh,
            () => MainWindow != null && MainWindow.Visibility == Visibility.Visible,
            () =>
            {
				MainWindow.Browser.ItemPanel.Refresh();
				MainWindow.Browser.Refresh();
            },
			toggle: null, icon: "Refresh");


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Configuration panel commands 

        // -------------------------------------------
        public static LimeCommand AddApp = new LimeCommand(
            null,
            () => {
                // Show the dialog
                var app = AppDialog(false);
                if (app != null)
                {
                    LimeMsg.Debug("AddApp: {0}", app);
                    //if (CfgWindow != null && CfgWindow.wxApp != null && CfgWindow.wxApp.AppTree != null)
                    //{
                    //    CfgWindow.wxApp.AppTree.LinkPaths(new string[] { app }, CfgWindow.Handle);
                    //    CfgWindow.wxApp.AppTree.Refresh();
                    //}
                    LimeLib.FreePIDL(app);
                }
            },
			toggle: null, icon: "AddApp");


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Lime Item commands 

        // -------------------------------------------
        public static LimeCommand ItemMenu = new LimeCommand( ApplicationCommands.ContextMenu,
            () => MainWindow.Browser.GetFocus != null,
            () => {
                MainWindow.Browser.LimeItem_ContextMenuOpening();
            });

		// -------------------------------------------
		public static LimeCommand ItemOpen = new LimeCommand(
			() => MainWindow.Browser.GetFocus != null,
			() =>
			{
				MainWindow.Browser.LimeItem_Click();
			}, toggle: null, icon: "Load");

		// -------------------------------------------
		public static LimeCommand ItemReload = new LimeCommand(
			() => Global.Local.SelectedItem != null && !Global.Local.SelectedItem.IsBusy,
			() =>
			{
				Global.Local.SelectedItem.Refresh();
				if (Global.Local.SelectedItem == MainWindow.Browser.ItemPanel)
				{
					MainWindow.Browser.Refresh();
				}
				else
				{
					Global.Local.SelectedItem.IconLoad();
				}
			},
			toggle: null, icon: "FileReload");

		// -------------------------------------------
		public static LimeCommand ItemSave = new LimeCommand( ApplicationCommands.Save,
			() => Global.Local.SelectedItem != null && Global.Local.SelectedItem.Modified && !Global.Local.SelectedItem.IsBusy,
			() =>
			{
                Task.Run(() => Global.Local.SelectedItem.SaveAsync());
            },
			toggle: null, icon: "Save");

		// -------------------------------------------
		public static LimeCommand ItemAdd = new LimeCommand(
            () => Global.Local.OnTop && MainWindow != null && !MainWindow.Browser.ItemPanel.ReadOnly,
            () => {
				FolderBrowser fb = new FolderBrowser
				{
					Description = Global.Properties["ItemAdd"].Desc,
					IncludeFiles = true,
					InitialDirectory = _ItemAddFolder
				};

				// Show the dialog
				if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LimeMsg.Debug("ItemAdd: {0}", fb.SelectedPath);
                    MainWindow.Browser.ItemPanel?.LinkPaths(new string[] { fb.SelectedPath });
                    if (_ItemAddFolder != fb.SelectedPath) LimeLib.FreePIDL(_ItemAddFolder);
                    _ItemAddFolder = fb.SelectedPath;
                }

            },
			toggle: null, icon: "AddFile");
        static string _ItemAddFolder = null;


        // -------------------------------------------
        public static LimeCommand ItemAddApp = new LimeCommand(
            () => Global.Local.OnTop && MainWindow != null && !MainWindow.Browser.ItemPanel.ReadOnly,
            () => {

                // Show the dialog
                var app = AppDialog();
                if (app != null)
                {
                    LimeMsg.Debug("ItemAddApp: {0}", app);
                    MainWindow.Browser.ItemPanel?.LinkPaths(new string[] { app });
                    LimeLib.FreePIDL(app);
                }

            },
			toggle: null, icon: "AddApp");

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Application Menu commands 

        // -------------------------------------------
        public static LimeCommand ConfigShow = new LimeCommand( ApplicationCommands.Properties,
            () => !Global.Local.ConfigVisible || Global.User.ConfigWindow,
            () => {
                if (CfgWindow == null)
                {
                    Global.Local.ConfigVisible = true;
                }
                else
                {
                    CfgWindow.Activate();
                }
            },
			toggle: null, icon: "Gear");


        // -------------------------------------------
        public static LimeCommand ConfigClose = new LimeCommand(  ApplicationCommands.Stop,
            () => Global.Local.ConfigVisible,
            () => {
                Global.Local.ConfigVisible = false;
            },
			toggle: null, icon: "Close");


		// -------------------------------------------
		public static LimeCommand Show = new LimeCommand(
            () => MainWindow.Visibility != Visibility.Visible || MainWindow.WindowState == WindowState.Minimized || !Global.Local.OnTop,
            () => {
                MainWindow.Show();

                // If CLI mode, make sure the screen is woken up
                if (Global.Local.CtrlMode == CtrlMode.CLI)
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.CONTROL);
            },
            toggle: nameof(Hide), icon: "WinShow");


		// -------------------------------------------
		public static LimeCommand Hide = new LimeCommand(ApplicationCommands.Stop,
			() => MainWindow.Visibility == Visibility.Visible && MainWindow.WindowState != WindowState.Minimized,
			() => {
				MainWindow.Hide();
			},
			toggle: nameof(Show), icon: "WinMinimize");


		// -------------------------------------------
		public static LimeCommand Normal = new LimeCommand(
			() => MainWindow.WindowState != WindowState.Normal,
			() => {
				Global.Local.WindowState = WindowState.Normal;
			},
			toggle: nameof(Maximize), icon: "WinRestore");


		// -------------------------------------------
		public static LimeCommand Maximize = new LimeCommand(
            () => MainWindow.WindowState != WindowState.Maximized,
            () => {
                 Global.Local.WindowState =  WindowState.Maximized;
            },
            toggle: nameof(Normal), icon: "WinMaximize");


        // -------------------------------------------
        public static LimeCommand Exit = new LimeCommand( 
            () => !Closing,
            () => {
                Closing = true;
                var wcfg = WPF.GetWindowPlacement(MainWindow.Handle);

                if (Global.User.HideOnLaunch) // Start Minimized, save state when shown
                    wcfg.WindowState = MainWindow.WindowStateShow; 

                Global.Local.WindowMain = wcfg;
                if (Global.User.Modified || Global.Local.Modified)
                {
                    var result = MessageBox.Show(
                        MainWindow,
                        LimeLanguage.Translate("Messages", "CfgChgSave", "Configuration has changed. Do you want to save it before you quit ?"),
                        "Lime Launcher",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Warning
                        );

                    if (result == MessageBoxResult.Yes)
                    {
                        Global.User.Save();
                        Global.Local.Save();
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        Closing = false;
                    }
                }
                else
                {
                    // Save Xml only, do not apply system changes
                    Global.Local.Save(null, false);
                }

                if(Closing) Application.Current.Shutdown();
            },
			toggle: null, icon: "Close");


        // -------------------------------------------
        [@LimeProperty(Visible = false)]
        public static LimeCommand ShowHide = new LimeCommand(Hide, toggle: true);


        #endregion

        #endregion

    }

}
