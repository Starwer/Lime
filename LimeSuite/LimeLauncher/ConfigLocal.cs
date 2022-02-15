/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     19-04-2016
* Copyright :   Sebastien Mouy © 2016
**************************************************************************/

using System;
using System.IO;
using System.Xml.Serialization;
using Lime;
using WPFhelper;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LimeLauncher.Controls;
using System.Runtime.Versioning;

namespace LimeLauncher
{

    /// <summary>
    /// Contains the Local configuration of LimeLauncher (machine-related).
    /// This exclude LimeItems, files and metadata.
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    [XmlRoot("LimeConfig")]
    public class ConfigLocal : LimeConfig
    {

        // --------------------------------------------------------------------------------------------------
        #region Types

        /// <summary>
        /// Store the Screen-specific parameters
        /// </summary>
        public class ScreenParameterType
        {
            [XmlIgnore]
            public bool Enabled;

            public double Scale;
            public double FullScreenBorderSize;
            public PaneSizeType PaneSizeNormal;
			public PaneSizeType PaneSizeFullScreen;

			public ScreenParameterType()
            {
				SaveScreen();
			}

            public void SetScreen()
            {
                if (Global.Local != null)
                {
                    Global.Local.modify = false;
                    Global.Local.Scale = Scale;
                    Global.Local.FullScreenBorderSize = FullScreenBorderSize;
                    Global.Local.PaneSizeNormal = PaneSizeNormal;
					Global.Local.PaneSizeFullScreen = PaneSizeFullScreen;
					Global.Local.modify = true;
                }
            }

			public void SaveScreen()
			{
				if (Global.Local != null)
				{
					Scale = Global.Local.Scale;
					FullScreenBorderSize = Global.Local.FullScreenBorderSize;
					PaneSizeNormal = new PaneSizeType(Global.Local.PaneSizeNormal);
					PaneSizeFullScreen = new PaneSizeType(Global.Local.PaneSizeFullScreen);
				}
			}
        }


		/// <summary>
		/// Store the different dimension of each panels
		/// </summary>
		public class PaneSizeType
		{
			public double InfoEdit { get; set; } = -1;
			public double MetadataSearch { get; set; } = -1;

			public PaneSizeType()
			{
			}

			public PaneSizeType (PaneSizeType copy)
			{
				if (copy != null) LimeLib.CopyPropertyValues(copy, this);
			}
		}


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Static Properties

		/// <summary>
		/// Path of the LimeLauncher User settings or Shell Link
		/// </summary>
		public static string SettingsPath
        {
            get { return LimeLib.ResolvePath(Path.Combine(About.ConfigPath, "LimeLauncher Local.xml")); }
        }

        /// <summary>
        /// Path of the Lime root Items directory or Shell Link
        /// </summary>
        public static string DataPath
        {
            get { return LimeLib.ResolvePath(Path.Combine(About.ConfigPath, "Data")); }
        }

        /// <summary>
        /// Path of the Lime Persons Database directory or Shell Link
        /// </summary>
        public static string PersonsPath
        {
            get { return LimeLib.ResolvePath(Path.Combine(About.ConfigPath, "Persons")); }
        }

        /// <summary>
        /// Path of the Lime root Items directory or Shell Link
        /// </summary>
        public static string ApplicationsPath
        {
            get { return LimeLib.ResolvePath(Path.Combine(About.ConfigPath, "Applications")); }
        }

        /// <summary>
        /// Path of the LimeLauncher User settings or Shell Link
        /// </summary>
        public static string AppsFolderPath
        {
            get { return Path.Combine(About.InstallPath, "AppsFolder.dat"); }
        }

        /// <summary>
        /// Path to the Default Directory template
        /// </summary>
        public static string TemplateDirPath
        {
            get
            {
                return Path.Combine(About.DocPath, "TemplateDir");
            }
        }

        /// <summary>
        /// Path to the Default File template
        /// </summary>
        public static string TemplateFilePath
        {
            get
            {
                return Path.Combine(TemplateDirPath, "TemplateFile");
            }
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Non-Observable Properties

        /// <summary>
        /// Enable to mark as modified some of the properties
        /// </summary>
        private bool modify = true;


		/// <summary>
		/// Software Unic Identifier
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = false)]
		public ulong SUID
		{
			get
			{
				return TagLib.Matroska.UIDElement.GenUID(_SUID);
			}

			set
			{
				_SUID = value;
			}
		}
		private ulong _SUID = 0;

		/// <summary>
		/// Store the position/state of the LimeLauncher main window
		/// </summary>
		public WPF.WindowPlacement WindowMain { get; set; }


		/// <summary>
		/// Store the position/state of the LimeLauncher Settings window
		/// </summary>
		public WPF.WindowPlacement WindowSettings { get; set; }

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Observable Properties

		/// <summary>
		/// Get or set the Control mode for the User Interface
		/// </summary>
		[XmlIgnore, LimePropertyAttribute(Visible = true)]
        public CtrlMode CtrlMode
        {
            get { return _CtrlMode; }
            set
            {
                if (value != _CtrlMode)
                {
                    _CtrlMode = value;
                    Commands.MainWindow?.RefreshCtrlMode(value);
                    OnPropertyChanged(false);
                }
            }
        }
        private CtrlMode _CtrlMode = CtrlMode.CLI;



        /// <summary>
        /// Instance of the current Skin
        /// </summary>
        [XmlIgnore]
        public Skin Skin
        {
            get { return _Skin; }
            set
            {
                if (value != _Skin)
                {
                    _Skin = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private Skin _Skin = null;


        /// <summary>
        /// Get or set the global GUI scale
        /// </summary>
        [XmlElement, LimePropertyAttribute(Minimum = 0.10, Maximum = 3.00, Percentage = true)]
        public double Scale
        {
            get { return _Scale; }
            set
            {
                // Coerce
                value = Coerce(value);


                if (value != _Scale)
                {
                    _Scale = ((int)(value * 100.0))/ 100.0;
                    _Zoom = _Scale;
                    Commands.MainWindow?.RenderZoom(false);

                    // Rescale all the objects of type Scaled
                    if (Global.User != null)
                    {
                        if (Global.User.EnableTypeScaled)
                        {
                            LimeMsg.Debug("Scale: using TypeScaled");
                            TypeScaled.Scale = _Scale;
                            TransformScale = 1.0;
                        }
                        else
                        {
                            LimeMsg.Debug("Scale: using Transform");
                            TypeScaled.Scale = 1.0;
                            TransformScale = _Scale;
                        }
                    }

					StoreScreenParameters();

					OnPropertyChanged(false);
                    OnPropertyChanged(false, "Zoom");
                }
            }
        }
        private double _Scale = 1.0;


        /// <summary>
        /// Get or set the global GUI Zoom
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Minimum = 0.10, Maximum = 3.00, Percentage=true, Visible = true)]
        public double Zoom
        {
            get { return _Zoom; }
            set
            {
                if (value != _Zoom)
                {
                    _Zoom = value;
                    Commands.MainWindow?.RenderZoom(true);
                    OnPropertyChanged(false);
                }
            }
        }
        private double _Zoom = 1.0;


        /// <summary>
        /// Set the factor of the LayoutTransform
        /// </summary>
        [XmlIgnore]
        public double TransformScale
        {
            get { return _TransformScale; }
            set
            {
                if (value != _TransformScale)
                {
                    _TransformScale = ((int)(value * 100.0)) / 100.0;
                    OnPropertyChanged(false);
                }
            }
        }
        private double _TransformScale = 1.0;



        /// <summary>
        /// Set the border thickness when in full-screen to compensate when borders on the screen (TV) are not visible.
        /// </summary>
        [XmlElement, LimePropertyAttribute(Maximum = 200)]
        public double FullScreenBorderSize
        {
            get { return _FullScreenBorderSize; }
            set
            {
                if (value != _FullScreenBorderSize)
                {
                    _FullScreenBorderSize = value;
					StoreScreenParameters();
					OnPropertyChanged(modify);
                }
            }
        }
        private double _FullScreenBorderSize = 10;


        /// <summary>
        /// Screen specific parameters
        /// </summary>
        [XmlElement]
        public bool ScreenSpecificParameters
        {
            get { return _ScreenSpecificParameters; }
            set
            {
                if (value != _ScreenSpecificParameters)
                {
                    _ScreenSpecificParameters = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _ScreenSpecificParameters = false;


        /// <summary>
        /// Screen Name: Set current screen, case-insensitive
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true), PickCollectionAttr("ScreenParameter", "Key")]
        public string ScreenName
        {
            get { return _ScreenName; }
            set
            {
                if (!string.Equals(value, _ScreenName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if(value == null)
                    {
                        _ScreenName = value;
                    }
                    else if (ScreenParameter != null && ScreenParameter.ContainsKey(value) && ScreenParameter[value].Enabled)
                    {
                        // Cosmetic: align case to screen-name case
                        foreach (string key in ScreenParameter.Keys)
                            if (string.Equals(value, key, StringComparison.InvariantCultureIgnoreCase))
                                value = key;

                        bool enableScreenMotion = _ScreenName != null;
                        _ScreenName = value;

                        // Load the Screen settings
                        if (ScreenSpecificParameters)
                        {
                            ScreenParameter[value].SetScreen();
                        }

                        // Move window to selected screen
                        var win = Commands.MainWindow;
                        if (win != null && enableScreenMotion)
                        {
                            win.MoveToScreen(value);                            
                        }
                        
                    }
                    else
                    {
                        LimeMsg.Error("ErrInvPropValue", "ScreenName", value);
                    }
                    if (value != null) OnPropertyChanged(false);
                }
            }
        }
        private string _ScreenName = null;


        /// <summary>
        /// Screen configuration. It misses some conversions to make it really read-able, but useful as event trigger
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true, ReadOnly = true)]
        public System.Windows.Forms.Screen[] ScreenConfig
        {
            get { return _ScreenConfig; }
            set
            {
                if (value != _ScreenConfig)
                {
                    _ScreenConfig = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private System.Windows.Forms.Screen[] _ScreenConfig = System.Windows.Forms.Screen.AllScreens;


        /// <summary>
        /// Store the Screen specific parameters
        /// </summary>
        [XmlElement, LimePropertyAttribute(Visible = false)]
        public SerializableDictionary<string, ScreenParameterType> ScreenParameter
        {
            get { return _ScreenParameter; }
            set
            {
                if (value != _ScreenParameter)
                {
                    _ScreenParameter = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private SerializableDictionary<string, ScreenParameterType> _ScreenParameter = null;


        /// <summary>
        /// Start with window option
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(ReqAdmin = true)]
        public bool StartWithWindows
        {
            get { return _StartWithWindows; }
            set
            {
                if (value != _StartWithWindows)
                {
                    _StartWithWindows = value;
                    StartWithWindowsModified = value != SystemRegistry.GetAutoRun(About.Name, About.ApplicationPath);
                    OnPropertyChanged();
                }
            }
        }
        private bool _StartWithWindows = SystemRegistry.GetAutoRun(About.Name, About.ApplicationPath);

        [XmlIgnore]
        public bool StartWithWindowsModified { get; private set; } = false;


        /// <summary>
        /// State of Main window
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true)]
        public WindowState WindowState
        {
            get { return _WindowState; }
            set
            {
                if (value != _WindowState)
                {
                    _WindowState = value;
					if (Commands.MainWindow != null)
					{
						OnPropertyChanged(false, "MetadataSearchPaneSize");
						Commands.MainWindow.UpdateWindowState();
						Commands.MainWindow.AdjustInfoPane(load: true);
					}
					OnPropertyChanged(false);
				}
			}
        }
        private WindowState _WindowState = WindowState.Minimized;


        /// <summary>
        /// Gets or sets whether the LimeLauncher window is in foreground. 
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true)]
        public bool OnTop
        {
            get { return _OnTop; }
            set
            {
                if (value != _OnTop)
                {
					_OnTop = value;
					if (value && Commands.MainWindow != null)
					{
						LimeMsg.Debug("---------- Bring OnTop: Task: {0}, Lime: {1}", Global.Local.TaskHandle, Commands.MainWindow.Handle);
						Win32.SetForegroundWindow(Commands.MainWindow.Handle);
					}
					LimeControl.AutoSlideReset(value);
					OnPropertyChanged(false);
                }
            }
        }
        private bool _OnTop = false;


        /// <summary>
        /// Position of the main Window
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true, ReadOnly = true)]
        public System.Drawing.Point WindowPosition
        {
            get { return _WindowPosition; }
            set
            {
                if (value != _WindowPosition)
                {
                    _WindowPosition = value;
                    OnPropertyChanged(false);

                }
            }
        }
        private System.Drawing.Point _WindowPosition = new System.Drawing.Point();


        /// <summary>
        /// Size of the main Window
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = false, ReadOnly = true)]
        public System.Drawing.Size WindowSize
        {
            get { return _WindowSize; }
            set
            {
                if (value != _WindowSize)
                {
                    _WindowSize = value;
                    OnPropertyChanged(false);

                }
            }
        }
        private System.Drawing.Size _WindowSize = new System.Drawing.Size();


        /// <summary>
        /// Current Screen DPI
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true, ReadOnly = true, Minimum = 0.10)]
        public double ScreenDPI
        {
            get { return _ScreenDPI; }
            set
            {
                // Coerce
                if (value < 0.1) value = 0.1;
                if (value != _ScreenDPI)
                {
                    _ScreenDPI = value;
                    OnPropertyChanged(false);

                }
            }
        }
        private double _ScreenDPI = 1.0;


        /// <summary>
        /// Active (foreground) task Handle
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true, ReadOnly = true)]
        public IntPtr TaskHandle
        {
            get { return _TaskHandle; }
            set
            {
                if (value != _TaskHandle)
                {
                    _TaskHandle = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private IntPtr _TaskHandle = IntPtr.Zero;



		/// <summary>
		/// Gets or sets whether the Info Pane must be shown, regardless of ShowInfoPaneAuto. 
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = true, Icon = "Info")]
		public bool ShowInfoPane
		{
			get { return _ShowInfoPane; }
			set
			{
				if (value != _ShowInfoPane)
				{
					_ShowInfoPane = value;
					Commands.MainWindow?.Refresh();
					OnPropertyChanged(false);
				}
			}
		}
		private bool _ShowInfoPane = false;


		/// <summary>
		/// Gets or sets whether the Info Pane is shown. 
		/// </summary>
		[XmlIgnore, LimePropertyAttribute(Visible = true, Icon = "Info")]
        public bool InfoPaneVisible
        {
            get { return _InfoPaneVisible; }
            set
            {
                if (value != _InfoPaneVisible)
                {
                    _InfoPaneVisible = value;
					Commands.MainWindow?.AdjustInfoPane(load: true);
					OnPropertyChanged(false);
                }
            }
        }
        private bool _InfoPaneVisible = false;


        /// <summary>
        /// Gets or sets whether the Info Pane is in Edit mode. 
        /// </summary>
        [XmlElement, LimePropertyAttribute(Visible = true, Icon = "Edit")]
		public bool InfoEditMode
		{
			get { return _InfoEditMode; }
			set
			{
				if (value != _InfoEditMode)
				{
					_InfoEditMode = value;
					if (SelectedItem == null && value) SelectedItem = FocusItem;
					Commands.MainWindow?.Refresh();
					OnPropertyChanged(false);
				}
			}
		}
		private bool _InfoEditMode = false;


        /// <summary>
        /// Gets or sets whether the Config Panel is visible. 
        /// </summary>
        [XmlIgnore, LimePropertyAttribute(Visible = true, Icon = "Gear")]
        public bool ConfigVisible
        {
            get { return _ConfigVisible; }
            set
            {
                if (value != _ConfigVisible)
                {
                    _ConfigVisible = value;
                    if (Global.User != null && Global.User.ConfigWindow)
                    {
                        if (value)
                        {
                            Commands.CfgWindow = new Configuration();
                            Commands.CfgWindow.Show();
                        }
                        else
                        {
                            var wobj = Commands.CfgWindow;
                            Commands.CfgWindow = null;
                            wobj.Close();
                            Commands.MainWindow?.Browser.TaskSwitcher.Refresh();
                        }
                    }
                    else if (Commands.MainWindow != null)
                    {
						// Try to preserve the focus on the windows
						Commands.MainWindow?.Browser?.wxToolBar?.SetFocus(Global.Properties[nameof(Global.Local.ConfigVisible)]);
						Commands.MainWindow?.Refresh();
					}
                    OnPropertyChanged(false);
                }
            }
        }
        private bool _ConfigVisible = false;


        /// <summary>
        /// Gets or sets whether the Metadata Search is shown. 
        /// </summary>
        [XmlElement, LimePropertyAttribute(Visible = true, Icon = "WebSearch")]
		public bool MetadataSearchVisible
		{
			get { return _MetadataSearchVisible; }
			set
			{
				if (value != _MetadataSearchVisible)
				{
					_MetadataSearchVisible = value;
					Commands.MainWindow?.AdjustInfoPane(load: true);
					OnPropertyChanged(false);
				}
			}
		}
		private bool _MetadataSearchVisible = false;


		/// <summary>
		/// Gets or sets the List View. 
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = true, Icon = "ListView")]
		public bool ListView
		{
			get { return _ListView; }
			set
			{
				if (value != _ListView)
				{
					_ListView = value;
					Global.Root.Tree.ImgSrcEnableBigSize = !ListView;
					Commands.MainWindow?.AdjustInfoPane(load: true);
					Commands.MainWindow?.Refresh();
					OnPropertyChanged(false);
				}
			}
		}
		private bool _ListView = false;


		/// <summary>
		/// Gets or sets the relative size of the Info Pane. 
		/// </summary>
		[XmlIgnore, LimePropertyAttribute(Visible = true, Minimum = 1, Maximum = 30)]
        public int MainPaneColumns
        {
            get
			{
				return _MainPaneColumns;
			}

            set
            {
                // Coerce
                value = (int)Coerce(value);

				if (value != _MainPaneColumns)
                {
					_MainPaneColumns = value;
					Commands.MainWindow?.AdjustInfoPane(setColumn: true);
					OnPropertyChanged(false);
					Commands.MainWindow?.Refresh();
				}
			}
        }
		private int _MainPaneColumns = 0;


		/// <summary>
		/// Gets or sets the size of the Info Picture Pane
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = true)]
		public double PicturePaneSize
		{
			get
			{
				return _PicturePaneSize;
			}
			set
			{
				if (value != _PicturePaneSize)
				{
					_PicturePaneSize = value;
					OnPropertyChanged(false);
				}
			}
		}
		private double _PicturePaneSize = -1;


		/// <summary>
		/// Get or Set the size of the Main Pan
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = true)]
		public double MainPaneSize
		{
			get
			{
				InitPaneSizes();

				return ListView ? PaneSizeListView.InfoEdit
					: WindowState != WindowState.Maximized ? PaneSizeNormal.InfoEdit
					: PaneSizeFullScreen.InfoEdit;
			}

			set
			{
				double prev = MainPaneSize;
				if (Math.Abs(value - prev) >= 1)
				{
					if (ListView) PaneSizeListView.InfoEdit = value;
					else if (WindowState != WindowState.Maximized) PaneSizeNormal.InfoEdit = value;
					else PaneSizeFullScreen.InfoEdit = value;

					StoreScreenParameters();
					OnPropertyChanged(false);
					Commands.MainWindow?.AdjustInfoPane(load: true);
				}
			}
		}

		/// <summary>
		/// Get or Set the size of the Metadata Search Pan
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = true)]
		public double MetadataSearchPaneSize
		{
			get
			{
				InitPaneSizes();

				return ListView ? PaneSizeListView.MetadataSearch
					: WindowState != WindowState.Maximized ? PaneSizeNormal.MetadataSearch
					: PaneSizeFullScreen.MetadataSearch;
			}

			set
			{
				double prev = MetadataSearchPaneSize;
				if (Math.Abs(value - prev) >= 1)
				{
					if (ListView) PaneSizeListView.MetadataSearch = value;
					else if (WindowState != WindowState.Maximized) PaneSizeNormal.MetadataSearch = value;
					else PaneSizeFullScreen.MetadataSearch = value;

					StoreScreenParameters();
					OnPropertyChanged(false);
				}
			}
		}


		/// <summary>
		/// Gets or sets the size of the Info Pane in windowed mode
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = false)]
		public PaneSizeType PaneSizeNormal
		{
			get
			{
				return _PaneSizeNormal;
			}
			set
			{
				if (value != _PaneSizeNormal)
				{
					_PaneSizeNormal = value;
					OnPropertyChanged(false, "MainPaneSize");
					OnPropertyChanged(false, "MetadataSearchPaneSize");
				}
			}
		}
		private PaneSizeType _PaneSizeNormal = null;

		/// <summary>
		/// Gets or sets the size of the Info Pane in Full Screen mode
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = false)]
		public PaneSizeType PaneSizeFullScreen
		{
			get
			{
				return _PaneSizeFullScreen;
			}
			set
			{
				if (value != _PaneSizeFullScreen)
				{
					_PaneSizeFullScreen = value;
					OnPropertyChanged(false, "MainPaneSize");
					OnPropertyChanged(false, "MetadataSearchPaneSize");
				}
			}
		}
		private PaneSizeType _PaneSizeFullScreen = null;

		/// <summary>
		/// Gets or sets the size of the Info Pane in List View mode. 
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = false)]
		public PaneSizeType PaneSizeListView
		{
			get
			{
				return _PaneSizeListView;
			}
			set
			{
				if (value != _PaneSizeListView)
				{
					_PaneSizeListView = value;
					OnPropertyChanged(false, "MainPaneSize");
                    OnPropertyChanged(false, "MetadataSearchPaneSize");
				}
			}
		}
		private PaneSizeType _PaneSizeListView = null;




		/// <summary>
		/// Gets or sets the focused element. 
		/// </summary>
		[XmlIgnore, LimePropertyAttribute(Visible = false)]
        public LimeItem FocusItem
        {
            get { return _FocusItem; }
            set
            {
                if (value != _FocusItem)
                {
                    _FocusItem = value;
					if (value != null)
					{
						OnPropertyChanged(false);
					}
				}
            }
        }
        private LimeItem _FocusItem = null;

		/// <summary>
		/// Gets or sets the selected element. 
		/// </summary>
		[XmlIgnore, LimePropertyAttribute(Visible = false)]
		public LimeItem SelectedItem
		{
			get { return _SelectedItem; }
			set
			{
				if (value != _SelectedItem)
				{
					_SelectedItem = value;
					LimeControl.AutoSlideReset();
					OnPropertyChanged(false);
				}
			}
		}
		private LimeItem _SelectedItem = null;

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors


		/// <summary>
		/// Initialize singleton class representing the Settings for LimeLauncher
		/// </summary>
		public ConfigLocal()
        {
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods

        /// <summary>
        /// Load the Settings of LimeLauncher from an XML file.
        /// </summary>
        /// <param name="path">Path of the file/link setting representation. Default: SettingsPath in the configuration path</param>
        /// <returns>The resulting ConfigLocal (singleton instance).</returns>
        public static ConfigLocal Load(string path = null)
        {
            if (String.IsNullOrEmpty(path)) path = SettingsPath;
            return Load<ConfigLocal>(path);
        }


        /// <summary>
        /// Save the Settings of LimeLauncher to an XML file.
        /// </summary>
        /// <param name="path">File to save the settings to.</param>
        /// <param name="apply">Apply pending changes to system (registry).</param>
        /// <returns>true if successful, false otherwise.</returns>
        public bool Save(string path = null, bool apply = true)
        {
            // Apply System changes
            if (StartWithWindowsModified)
            {
                LimeMsg.Debug("ConfigLocal: Set StartWithWindows: {0}", Global.Local.StartWithWindows);
                SystemRegistry.SetAutoRun(About.Name, Global.Local.StartWithWindows ? About.ApplicationPath : null);
                StartWithWindowsModified = false;
            }

            if (String.IsNullOrEmpty(path)) path = SettingsPath;
            return Save(path, this);
        }


        /// <summary>
        /// Store the screen-specific settings
        /// </summary>
        private void StoreScreenParameters()
		{
			if (ScreenParameter != null && ScreenName != null && ScreenParameter.ContainsKey(ScreenName))
			{
				ScreenParameter[ScreenName].SaveScreen();
			}
		}


		/// <summary>
		/// Make sure the Pane Sizes are properly initialized
		/// </summary>
		private void InitPaneSizes()
		{
			if (PaneSizeListView == null) PaneSizeListView = new PaneSizeType();
			if (PaneSizeNormal == null) PaneSizeNormal = new PaneSizeType();
			if (PaneSizeFullScreen == null) PaneSizeFullScreen = new PaneSizeType();
		}

		#endregion
	}
}
