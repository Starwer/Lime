/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     19-04-2016
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System;
using System.IO;
using System.Xml.Serialization;
using Lime;
using WPFhelper;
using System.Collections.Generic;
using LimeLauncher.Controls;

namespace LimeLauncher
{
    /// <summary>
    /// Contains the User-configuration of LimeLauncher (and basic LIME configuration).
    /// This exclude LimeItems, files and metadata.
    /// </summary>
    [XmlRoot("LimeConfig")]
    public class ConfigUser : LimeConfig
    {
		// --------------------------------------------------------------------------------------------------
		#region Types

        /// <summary>
        /// List the possible action on clicking border
        /// </summary>
        public enum ClickOnBorderActionType
        {
            DoNothing,
            Hide,
            Restore
        }

		/// <summary>
		/// List the condition to automatically show/hide the Info Pane
		/// </summary>
		public enum ShowInfoPaneAutoType
		{
			Never,
			WhenInMediaDirectory,
			WhenDirectoryContainsMedia
		}


		/// <summary>
		/// Specify the default operation which can be accomplished in Drag and drop.
		/// Convertible to a <see cref="ClipDragDefaultOperation"/> 
		/// </summary>
		public enum DragDropActionType
		{
			/// <summary>
			/// Copy by default
			/// </summary>
			Copy = 0x01,

			/// <summary>
			/// Move by default
			/// </summary>
			Move = 0x02,

			/// <summary>
			/// Create a link by default
			/// </summary>
			Link = 0x04,

			/// <summary>
			/// Show a drop-menu by default
			/// </summary>
			Menu = 0x08

		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Static Properties

		/// <summary>
		/// Path of the LimeLauncher User settings or Shell Link
		/// </summary>
		public static string SettingsPath
		{
			get { return LimeLib.ResolvePath(Path.Combine(About.ConfigPath, "LimeLauncher User.xml")); }
		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Non-Observable Properties

		/// <summary>
		/// User Unic Identifier
		/// </summary>
		[XmlElement, LimePropertyAttribute(Visible = false)]
		public ulong UUID
		{
			get
			{
				return TagLib.Matroska.UIDElement.GenUID(_UUID);
			}

			set
			{
				_UUID = value;
			}
		}
		private ulong _UUID = 0;

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Observable Properties
		// All the properties marked as XmlElement will automatically be accessible/translated in XAML,
		// and saved in XML file pointed by SettingsPath.
		// Still, the OnPropertyChanged must be called by each observable property

		/// <summary>
		/// Interface language
		/// </summary>
		[XmlElement, LimePropertyAttribute(ReqRestart = true)]
        public LimeLanguage Language
        {
            get { return _Language; }
            set
            {
                if (value != _Language)
                {
                    _Language = value;
                    OnPropertyChanged();

					var lang = string.IsNullOrEmpty(MetadataLanguage) ? Language.Key : LimeLib.FirstWord(MetadataLanguage);
					XmlMetadataLanguage = System.Windows.Markup.XmlLanguage.GetLanguage(lang);
					OnPropertyChanged(modified: false, name: nameof(XmlMetadataLanguage));
				}
			}
        }
        private LimeLanguage _Language = new LimeLanguage();


        /// <summary>
        /// Define whether the main window of LimeLauncher is always on top.
        /// </summary>
        [XmlElement]
        public bool TopMost
        {
            get { return _TopMost; }
            set
            {
                if (value != _TopMost)
                {
                    _TopMost = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _TopMost = true;


        /// <summary>
        /// Show Lime in Taskbar.
        /// </summary>
        [XmlElement]
        public bool ShowInTaskbar
        {
            get { return _ShowInTaskbar; }
            set
            {
                if (value != _ShowInTaskbar)
                {
                    _ShowInTaskbar = value;
                    if(value && Commands.MainWindow != null && Commands.MainWindow.Visibility != System.Windows.Visibility.Visible)
                    {
                        Commands.MainWindow.Visibility = System.Windows.Visibility.Visible;
                    }
                    OnPropertyChanged();
                }
            }
        }
        private bool _ShowInTaskbar = false;


        /// <summary>
        /// Hide LimeLauncher when launching
        /// </summary>
        [XmlElement]
        public bool HideOnLaunch
        {
            get { return _HideOnLaunch; }
            set
            {
                if (value != _HideOnLaunch)
                {
                    _HideOnLaunch = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _HideOnLaunch = true;


        /// <summary>
        /// Default action on File/directory Drag & Drop
        /// </summary>
        [XmlElement]
        public DragDropActionType DragDropDefault
        {
            get { return _DragDropDefault; }
            set
            {
                if (value != _DragDropDefault)
                {
                    _DragDropDefault = value;
					OnPropertyChanged();
					OnPropertyChanged(modified: false, name: nameof(DragDefaultOperation));
				}
			}
        }
        private DragDropActionType _DragDropDefault = DragDropActionType.Menu;

		/// <summary>
		/// Default action on File/directory Drag & Drop in WPF
		/// </summary>
		[XmlIgnore]
		public ClipDragDefaultOperation DragDefaultOperation
		{
			get { return (ClipDragDefaultOperation)_DragDropDefault; }
		}

		/// <summary>
		/// Action when clicking on boder in Fullscreen
		/// </summary>
		[XmlElement]
        public ClickOnBorderActionType ClickOnBorderAction
        {
            get { return _ClickOnBorderAction; }
            set
            {
                if (value != _ClickOnBorderAction)
                {
                    _ClickOnBorderAction = value;
                    OnPropertyChanged();
                }
            }
        }
        private ClickOnBorderActionType _ClickOnBorderAction = ClickOnBorderActionType.Hide;


        /// <summary>
        /// Define whether the main window of LimeLauncher has borders.
        /// </summary>
        [XmlElement, LimePropertyAttribute(ReqRestart = true)]
        public bool ShowWindowBorders
        {
            get { return _ShowWindowBorders; }
            set
            {
                if (value != _ShowWindowBorders)
                {
                    if (Commands.MainWindow != null && !Commands.MainWindow.AllowsTransparency)
                    {
                        Commands.MainWindow.WindowStyle = value ? System.Windows.WindowStyle.SingleBorderWindow : System.Windows.WindowStyle.None;
                    }

                    _ShowWindowBorders = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _ShowWindowBorders = false;

        /// <summary>
        /// Define whether the main window of LimeLauncher has border also in fullscreen.
        /// </summary>
        [XmlElement]
        public bool ShowWindowBordersFullScreen
        {
            get { return _ShowWindowBordersFullScreen; }
            set
            {
                if (value != _ShowWindowBordersFullScreen)
                {
                    if (Commands.MainWindow != null && !Commands.MainWindow.AllowsTransparency && Commands.MainWindow.WindowState == System.Windows.WindowState.Maximized)
                    {
                        Commands.MainWindow.WindowStyle = value ? System.Windows.WindowStyle.SingleBorderWindow : System.Windows.WindowStyle.None;
                    }

                    _ShowWindowBordersFullScreen = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _ShowWindowBordersFullScreen = false;


        /// <summary>
        /// Show task-Switcher.
        /// </summary>
        [XmlElement]
        public bool ShowTaskSwitcher
        {
            get { return _ShowTaskSwitcher; }
            set
            {
                if (value != _ShowTaskSwitcher)
                {
                    _ShowTaskSwitcher = value;
                    if (Commands.MainWindow != null && Commands.MainWindow.Browser.TaskSwitcher != null)
                    {
                        Commands.MainWindow.Browser.TaskSwitcher.IsPanelVisible = value;
                        if (value || TaskMatchEnable)
                            Commands.MainWindow.Browser.TaskSwitcher.Refresh();
                        else
                            Commands.MainWindow.Browser.TaskSwitcher.Clear();

                    }
                    OnPropertyChanged();
                }
            }
        }
        private bool _ShowTaskSwitcher = true;

        /// <summary>
        /// Show Applications in task-Switcher.
        /// </summary>
        [XmlElement]
        public bool ShowAppTasks
        {
            get { return _ShowAppTasks; }
            set
            {
                if (value != _ShowAppTasks)
                {
                    _ShowAppTasks = value;
                    Global.Root.Tree.TaskSwitcherShowApp = value;
                    if (Commands.MainWindow != null 
                        && Commands.MainWindow.Browser.TaskSwitcher != null 
                        && Commands.MainWindow.Browser.TaskSwitcher.Children != null)
                        Commands.MainWindow.Browser.TaskSwitcher.Refresh();
                    OnPropertyChanged();
                }
            }
        }
        private bool _ShowAppTasks = true;

        /// <summary>
        /// Open Config in separate window
        /// </summary>
        [XmlElement]
        public bool ConfigWindow
        {
            get { return _ConfigWindow; }
            set
            {
                if (value != _ConfigWindow)
                {
                    if (Commands.MainWindow != null)
                    {
                        // Close the config
                        bool visible = Global.Local.ConfigVisible;
                        Global.Local.ConfigVisible = false;

                        // Reopen it in the right mode if this was open
                        _ConfigWindow = value;
                        Global.Local.ConfigVisible = visible;
                        Commands.MainWindow.Refresh();

                        OnPropertyChanged();
                        if (Global.Local.ConfigVisible) Commands.ConfigShow.Execute();
                    }
                    else
                    {
                        _ConfigWindow = value;
                        OnPropertyChanged();
                    }
                }
            }
        }
        private bool _ConfigWindow = false;


        /// <summary>
        /// Enable the Task-Matcher.
        /// </summary>
        [XmlElement]
        public bool TaskMatchEnable
        {
            get { return _TaskMatchEnable; }
            set
            {
                if (value != _TaskMatchEnable)
                {
                    _TaskMatchEnable = value;
                    if (Commands.MainWindow != null)
                    {
                        if (value || ShowTaskSwitcher)
                            Commands.MainWindow.Browser.TaskSwitcher.Refresh();
                        else
                            Commands.MainWindow.Browser.TaskSwitcher.Clear();
                        Commands.MainWindow.Refresh();
                    }
                    OnPropertyChanged();
                }
            }
        }
        private bool _TaskMatchEnable = true;


		/// <summary>
		/// Enable the use of Left/Right keys to navigate.
		/// </summary>
		[XmlElement]
        public bool KeysToNavigateEnable
        {
            get { return _KeysToNavigateEnable; }
            set
            {
                if (value != _KeysToNavigateEnable)
                {
                    _KeysToNavigateEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _KeysToNavigateEnable = true;


        /// <summary>
        /// Enable the use Double-Click to toggle Full-screen.
        /// </summary>
        [XmlElement]
        public bool DoubleClickFullScreen
        {
            get { return _DoubleClickFullScreen; }
            set
            {
                if (value != _DoubleClickFullScreen)
                {
                    _DoubleClickFullScreen = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _DoubleClickFullScreen = true;


		/// <summary>
		/// Enable the use of System menu.
		/// </summary>
		[XmlElement]
		public bool SystemMenuEnable
		{
			get { return _SystemMenuEnable; }
			set
			{
				if (value != _SystemMenuEnable)
				{
					_SystemMenuEnable = value;
					OnPropertyChanged();
				}
			}
		}
		private bool _SystemMenuEnable = true;


		/// <summary>
		/// Enable the animations
		/// </summary>
		[XmlElement]
        public bool EnableAnimations
        {
            get { return _EnableAnimations; }
            set
            {
                if (value != _EnableAnimations)
                {
                    _EnableAnimations = value;

                    if (Global.Local != null && Global.Local.OnTop)
                    {
                        AnimateAction.EnableAnimations = value;
                    }

                    OnPropertyChanged();
                }
            }
        }
        private bool _EnableAnimations = true;



		/// <summary>
		/// Name of the root data directory
		/// </summary>
		[XmlElement]
        public string RootDirectoryName
        {
            get { return _RootDirectoryName; }
            set
            {
                if (value != _RootDirectoryName)
                {
                    _RootDirectoryName = value;
                    if (_RootDirectoryName != null) Global.Root.Name = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _RootDirectoryName = "Data";


        /// <summary>
        /// Set the skin-name to be used
        /// </summary>
        [XmlElement, PickCollectionAttr("SkinList")]
        public string Skin
        {
            get { return _Skin; }
            set
            {
                if (value != _Skin)
                {
                    _Skin = value;

                    if (Commands.MainWindow != null)
                    {
                        Commands.MainWindow.LoadSkin(value);
                    }

                    OnPropertyChanged();
                }
            }
        }
        private string _Skin;


        [XmlIgnore]
        public string[] SkinList
        {
            get { return _SkinList; }
            set
            {
                if (value != _SkinList)
                {
                    _SkinList = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private string[] _SkinList;


        /// <summary>
        /// Store the Skin parameters for every available skins
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, SkinParam[]> SkinParams
        {
            get { return _SkinParams; }
            set
            {
                if (value != _SkinParams)
                {
                    _SkinParams = value;
                    OnPropertyChanged(modified: false);
                }
            }
        }
        private Dictionary<string, SkinParam[]> _SkinParams = null;


        /// <summary>
        /// Enable to Serialize the SkinParams Dictionary
        /// </summary>
        [XmlElement, LimePropertyAttribute(Visible = false)]
        public string SkinParamsSerializer
        {
            get
            {
                if (_SkinParams == null) return null;

                string str = Environment.NewLine;
                foreach (var skin in _SkinParams)
                {
                    str += "  " + skin.Key + Environment.NewLine;
                    foreach (var param in skin.Value)
                    {
                        if(param.Content != null)
                            str += "    " + param.Ident + ":" + param.Serialize + Environment.NewLine;
                    }
                    str += Environment.NewLine;
                }

                return str + "  ";
            }

            set
            {
                var ret = new Dictionary<string, SkinParam[]>();
                try
                {
                    var lst = value.Split('\n');
                    if (lst.Length > 1)
                    {
                        var param = new List<SkinParam>();
                        string skin = null;
                        foreach (var line in lst)
                        {
                            int idx = line.IndexOf(':');
                            if (idx > 0)
                            {
								var par = new SkinParam
								{
									Ident = line.Substring(0, idx).Trim(),
									Serialize = line.Substring(idx + 1)
								};
								param.Add(par);
                            }
                            else
                            {
                                var sk = line.Trim();
                                if (sk.Length > 0)
                                {
                                    if (skin != null && param.Count > 0) ret.Add(skin, param.ToArray());
                                    param = new List<SkinParam>();
                                    skin = sk;
                                }
                            }
                        }

                        if(skin!=null && param.Count>0) ret.Add(skin, param.ToArray());
                    }

                    SkinParams = ret;
                }
                catch { }
            }
        }


		/// <summary>
		/// Skin auto-refesh (Developper mode only)
		/// </summary>
		[XmlElement]
		public bool SkinAutoRefresh
		{
			get { return _SkinAutoRefresh; }
			set
			{
				if (value != _SkinAutoRefresh)
				{
					_SkinAutoRefresh = value;
					OnPropertyChanged();
					if (value)
					{
						Commands.SkinReload.Execute();
					}
				}
			}
		}
		private bool _SkinAutoRefresh = false;


		/// <summary>
		/// Enable the Developper Mode
		/// </summary>
		[XmlElement]
        public bool DevMode
        {
            get { return _DevMode; }
            set
            {
                if (value != _DevMode)
                {
                    _DevMode = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _DevMode = false;


        /// <summary>
        /// Enable the use of TypeScaled for scaling, instead of using Transform
        /// </summary>
        [XmlElement]
        public bool EnableTypeScaled
        {
            get { return _EnableTypeScaled; }
            set
            {
                if (value != _EnableTypeScaled)
                {
                    _EnableTypeScaled = value;

                    // Rescale all the objects of type Scaled
                    if (Global.Local != null)
                    {
                        if (value)
                        {
                            LimeMsg.Debug("EnableTypeScaled: True: TypeScaled");
                            TypeScaled.Scale = Global.Local.Scale;
                            Global.Local.TransformScale = 1.0;
                        }
                        else
                        {
                            LimeMsg.Debug("EnableTypeScaled: False: Transform");
                            TypeScaled.Scale = 1.0;
                            Global.Local.TransformScale = Global.Local.Scale;
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }
        private bool _EnableTypeScaled = true;

        /// <summary>
        /// Enable to create a Shell link to a link/URL
        /// </summary>
        [XmlElement]
        public bool AllowLinkOfLink
        {
            get { return Global.Root.Tree.AllowLinkOfLink; }
            set
            {
                if (value != Global.Root.Tree.AllowLinkOfLink)
                {
                    Global.Root.Tree.AllowLinkOfLink = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Enable to round-off the size of the icons to get sharper rendering (but less accurate sizing)
        /// </summary>
        [XmlElement]
        public bool SmartIconSize
        {
            get { return _SmartIconSize; }
            set
            {
                if (value != _SmartIconSize)
                {
                    _SmartIconSize = value;
                    Global.Root.Tree.ImgSrcSizeRoundOff = value;
                    Commands.MainWindow?.Refresh();
                    OnPropertyChanged();
                }
            }
        }
        private bool _SmartIconSize = true;


		/// <summary>
		/// Use Cover for icons instead of the Windows icon
		/// </summary>
		[XmlElement]
		public bool IconCover
		{
			get { return Global.Root.Tree.ImgSrcUseCover; }
			set
			{
				if (value != Global.Root.Tree.ImgSrcUseCover)
				{
					Global.Root.Tree.ImgSrcUseCover = value;
					Commands.MainWindow?.Refresh();
					OnPropertyChanged();
				}
			}
		}


		/// <summary>
		/// Apply Zoom after this delay in Milliseconds
		/// </summary>
		[XmlElement, LimePropertyAttribute(Minimum = 10)]
        public uint ApplyZoomAfter
        {
            get { return _ApplyZoomAfter; }
            set
            {
                if (value != _ApplyZoomAfter)
                {
                    _ApplyZoomAfter = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _ApplyZoomAfter = 800;


		/// <summary>
		/// Update InfoPane after this delay in Milliseconds
		/// </summary>
		[XmlElement, LimePropertyAttribute(Minimum = 10)]
		public uint AutoSelectionAfter
		{
			get { return _AutoSelectionAfter; }
			set
			{
				if (value != _AutoSelectionAfter)
				{
					_AutoSelectionAfter = value;
					OnPropertyChanged();
				}
			}
		}
		private uint _AutoSelectionAfter = 500;


		/// <summary>
		/// Get or set in which Control-modes the Auto-selection is enabled
		/// </summary>
		[XmlElement]
		public CtrlMode AutoSelection
		{
			get { return _AutoSelection; }
			set
			{
				if (value != _AutoSelection)
				{
					_AutoSelection = value;
					OnPropertyChanged();
				}
			}
		}
		private CtrlMode _AutoSelection = CtrlMode.Mouse | CtrlMode.Keyboard;



		/// <summary>
		/// Enable Auto-slide. 
		/// </summary>
		[XmlElement]
		public bool AutoSlide
		{
			get { return LimeControl.AutoSlideEnable; }
			set
			{
				if (value != LimeControl.AutoSlideEnable)
				{
					LimeControl.AutoSlideEnable = value;
					OnPropertyChanged();
				}
			}
		}


		/// <summary>
		/// Slide-Show period in Seconds
		/// </summary>
		[XmlElement, LimePropertyAttribute(Minimum = 1, Maximum = 600)]
		public uint SlideShowPeriod
		{
			get { return LimeControl.SlideShowPeriod; }
			set
			{
				if (value != LimeControl.SlideShowPeriod)
				{
					LimeControl.SlideShowPeriod = value;
					OnPropertyChanged();
				}
			}
		}


		/// <summary>
		/// Show Info Pane automatically
		/// </summary>
		[XmlElement]
		public ShowInfoPaneAutoType ShowInfoPaneAuto
		{
			get { return _ShowInfoPaneAuto; }
			set
			{
				if (value != _ShowInfoPaneAuto)
				{
					_ShowInfoPaneAuto = value;
					Commands.MainWindow?.Refresh();
					OnPropertyChanged();
				}
			}
		}
		private ShowInfoPaneAutoType _ShowInfoPaneAuto = ShowInfoPaneAutoType.WhenDirectoryContainsMedia;



		/// <summary>
		/// Info Pane shows automatically for these media types
		/// </summary>
		[XmlElement]
		public MediaType ShowInfoPaneMediaTypes
		{
			get { return _ShowInfoPaneMediaTypes; }
			set
			{
				if (value != _ShowInfoPaneMediaTypes)
				{
					_ShowInfoPaneMediaTypes = value;
					Commands.MainWindow?.Refresh();
					OnPropertyChanged();
				}
			}
		}
		private MediaType _ShowInfoPaneMediaTypes = MediaType.Video | MediaType.Audio | MediaType.Image;



		/// <summary>
		/// List of file extensions recognized as video, comma-separated
		/// </summary>
		[XmlElement]
		public string ExtensionsVideo
		{
			get { return LimeLib.DeltaListFrom(LimeLib.VideoExtensions, LimeLib.cstVideoExtensions); }
			set
			{
				LimeLib.VideoExtensions = LimeLib.DeltaListParse(value, LimeLib.cstVideoExtensions);
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// List of file extensions recognized as video, comma-separated
		/// </summary>
		[XmlElement]
		public string ExtensionsAudio
		{
			get { return LimeLib.DeltaListFrom(LimeLib.AudioExtensions, LimeLib.cstAudioExtensions); }
			set
			{
				LimeLib.AudioExtensions = LimeLib.DeltaListParse(value, LimeLib.cstAudioExtensions);
				OnPropertyChanged();
			}

		}
		
		/// <summary>
		/// List of file extensions recognized as video, comma-separated
		/// </summary>
		[XmlElement]
		public string ExtensionsImage
		{
			get { return LimeLib.DeltaListFrom(LimeLib.ImageExtensions, LimeLib.cstImageExtensions); }
			set
			{
				LimeLib.ImageExtensions = LimeLib.DeltaListParse(value, LimeLib.cstImageExtensions);
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// List of file extensions recognized as video, comma-separated
		/// </summary>
		[XmlElement]
		public string ExtensionsDocument
		{
			get { return LimeLib.DeltaListFrom(LimeLib.DocumentExtensions, LimeLib.cstDocumentExtensions); }
			set
			{
				LimeLib.DocumentExtensions = LimeLib.DeltaListParse(value, LimeLib.cstDocumentExtensions);
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Metadata Prefered Language
		/// </summary>
		[XmlElement]
		public string MetadataLanguage
		{
			get { return Global.Search.Language; }
			set
			{
				if (value != Global.Search.Language)
				{
					Global.Search.Language = value;
					OnPropertyChanged();

					var lang = string.IsNullOrEmpty(MetadataLanguage) ? Language.Key : LimeLib.FirstWord(MetadataLanguage);
					XmlMetadataLanguage = System.Windows.Markup.XmlLanguage.GetLanguage(lang);
					OnPropertyChanged(modified: false, name: nameof(XmlMetadataLanguage));
				}
			}
		}


		/// <summary>
		/// Metadata Prefered Language Selected
		/// </summary>
		[XmlIgnore]
		public System.Windows.Markup.XmlLanguage XmlMetadataLanguage { get; private set; } = null;


		/// <summary>
		/// Enable to show adult matches in the search. 
		/// </summary>
		[XmlElement]
		public bool Adult
		{
			get { return Global.Search.Adult; }
			set
			{
				if (value != Global.Search.Adult)
				{
					Global.Search.Adult = value;
					OnPropertyChanged();
				}
			}
		}


		/// <summary>
		/// Enable the spell checker. 
		/// </summary>
		[XmlElement]
		public bool SpellCheck
		{
			get { return _SpellCheck; }
			set
			{
				if (value != _SpellCheck)
				{
					_SpellCheck = value;
					OnPropertyChanged();
				}
			}
		}
		private bool _SpellCheck = true;


		/// <summary>
		/// Save Person info automatically. 
		/// </summary>
		[XmlElement]
        public bool PersonAutoSave
        {
            get { return LimePerson.AutoSave; }
            set
            {
                if (value != LimePerson.AutoSave)
                {
					LimePerson.AutoSave = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Download Person info automatically. 
        /// </summary>
        [XmlElement]
        public uint PersonAutoDownload
        {
            get { return _PersonAutoDownload; }
            set
            {
                if (value != _PersonAutoDownload)
                {
                    _PersonAutoDownload = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _PersonAutoDownload = 10;



		/// <summary>
		/// Setup replace pattern for making a movie search querie from a file-path. 
		/// </summary>
		[XmlElement]
		public ReplacerChain PathToSearchMovie
		{
			get { return _PathToSearchMovie; }
			set
			{
				if (value != _PathToSearchMovie)
				{
					_PathToSearchMovie = value;
					OnPropertyChanged();
				}
			}
		}
		private ReplacerChain _PathToSearchMovie = new ReplacerChain();
		private static readonly ReplacerChain PathToSearchMovieDefault = new ReplacerChain
		{
			new Replacer(@"^.*\\", "", ReplacerOptions.Regex | ReplacerOptions.IgnoreCase | ReplacerOptions.All),
			new Replacer(@"\.[^\.]+$", "", ReplacerOptions.Regex | ReplacerOptions.IgnoreCase | ReplacerOptions.All),
			new Replacer(@"[\._ ]+", " ", ReplacerOptions.Regex | ReplacerOptions.IgnoreCase | ReplacerOptions.All),
			new Replacer(@"\[.*\]", "", ReplacerOptions.Regex | ReplacerOptions.IgnoreCase | ReplacerOptions.All),
		};

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors


		/// <summary>
		/// Initialize singleton class representing the Settings for LimeLauncher
		/// </summary>
		public ConfigUser()
        {
            _Skin = LimeLauncher.Skin.DefaultSkin;
		}

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods

        /// <summary>
        /// Load the Settings of LimeLauncher from an XML file.
        /// </summary>
        /// <param name="path">Path of the file/link setting representation. Default: SettingsPath in the configuration path</param>
        /// <returns>The resulting ConfigUser (singleton instance).</returns>
        public static ConfigUser Load(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = SettingsPath;
            }

            var ret = Load<ConfigUser>(path);

			// Force default values in collections
			if (ret.PathToSearchMovie.Count == 0)
			{
				ret.PathToSearchMovie = PathToSearchMovieDefault;
			}

			return ret;

		}


        /// <summary>
        /// Save the Settings of LimeLauncher to an XML file.
        /// </summary>
        /// <param name="path">File to save the settings to.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public bool Save(string path = null)
        {
            if (String.IsNullOrEmpty(path))
            {
                path = SettingsPath;
            }

            return Save(path, this);
        }

        #endregion

    }
}
