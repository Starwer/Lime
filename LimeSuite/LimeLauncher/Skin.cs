/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     01-12-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using Lime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Serialization;
using WPFhelper;

namespace LimeLauncher
{


    // --------------------------------------------------------------------------------------------------
    #region WPF Types

    /// <summary>
    /// Alow convertion from virtually everything to everything to the SkinParam. The realcoonversion will be 
    /// handled by the ConfigProperty properties: Content/Value.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SkinParamConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type targetType)
        {
            return true;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type targetType)
        {
            if (targetType == null) throw new ArgumentNullException("targetType");


			if (value is SkinParam ret)
			{
				base.ConvertTo(context, culture, ret.Content, targetType);
			}

			return base.ConvertTo(context, culture, value, targetType);
        }
    }


    /// <summary>
    /// Encompasse an object in this Content property that should be parameterizable in Lime Launcher.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [Serializable]
    [ContentProperty("Content"), TypeConverter(typeof(SkinParamConverter))]
    public class SkinParam : LimeProperty
    {

        public static List<SkinParam> List { get; private set; } = new List<SkinParam>();

        private static bool Locked = false;


        /// <summary>
        /// Name of the Skin parameter as it should appear in the Configuration Panel 
        /// </summary>
        [XmlIgnore]
        public string Text
        {
            get { return Name; }
            set { Name = value; }
        }


        public SkinParam()
        {
            if (!Locked) List.Add(this);
        }

        public static void Clear()
        {
            List = new List<SkinParam>();
            Locked = false;
        }


        public static void Lock()
        {
            Locked = true;
        }

    }

    #endregion



    /// <summary>
    /// Represent the parameters of a skin for LimeLauncher
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public class Skin : INotifyPropertyChanged
    {

        // --------------------------------------------------------------------------------------------------
        #region Boilerplate

        // Boilerplate code for INotifyPropertyChanged : Instances

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Constants

        /// <summary>
        /// Default Skin
        /// </summary>
        public static readonly string DefaultSkin = "ProjectTile";


        /// <summary>
        /// Fallback Skin, in case of failure to load any skin
        /// </summary>
        public static readonly string FallbackSkin = "System";


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="skinName">Name of the skin</param>
        /// <param name="loadParam">Load the parameters from user-config</param>
        public Skin(string skinName, bool loadParam = true)
        {
            LimeMsg.Debug("Skin: constructor");
			LimeLib.LifeTrace(this);

            // Invalidate this skin
            Name = null;

            // Prepare to populate new set of Skin-parameters
            SkinParam.Clear();

            // Load Skin list
            LoadSkinList();

            // Load resource
            ResourceDictionary resources = null;
            if (!string.IsNullOrEmpty(skinName))
            {
				// Load Xaml here
				string dir = About.SkinsPath;
#if DEBUG
				// Debug Only: bypass local skins to use the one in the project directly
				if (!string.IsNullOrEmpty(Global.DebugProjectDir))
				{
					dir = Path.Combine(Global.DebugProjectDir, "Skins");
				}
#endif

				dir = Path.Combine(dir, skinName);
				dir = LimeLib.ResolvePath(dir);

                string path = Path.Combine(dir, skinName + ".xaml");
                path = LimeLib.ResolvePath(path);

				// Parse theXaml. This may fail if there is a file/Xaml error
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    // Get the root element, which must be a ResourceDictionary
                    resources = (ResourceDictionary)XamlReader.Load(fs);
                }

				// File change monitoring
				if (Global.User.SkinAutoRefresh && Global.User.DevMode)
				{
					LimeMsg.Debug("Skin: constructor: Watch changes on {0}", dir);
					_Watch = new FileSystemWatcher
					{
						Path = dir,
						IncludeSubdirectories = false,
						Filter = ""
					};

					_Watch.Changed += new FileSystemEventHandler(OnXamlFileChanged);

					_Watch.EnableRaisingEvents = true;
				}
				else
				{
					_Watch = null;
				}

			}
            
            // Freeze the Parameters
            SkinParam.Lock();


            // Retrieve/check Required resources
            double version = 0.0;
            try
            {
                MetaAuthor = (string)resources["MetaAuthor"];
                MetaContact = (string)resources["MetaContact"];
                MetaWebsite = (string)resources["MetaWebsite"];
                MetaDescription = (string)resources["MetaDescription"];
                version = (double)resources["MetaLimeVersion"];
            }
            catch (ResourceReferenceKeyNotFoundException e)
            {
                LimeMsg.Error("ErrSkinParMiss", e.Key);
            }
            catch
            {
                LimeMsg.Error("ErrSkinFormat");
            }

            if (version > 0.5)
            {
                // Successfull
                if (version > About.VersionNum)
                {
                    LimeMsg.Error("ErrSkinVersion", skinName, About.Name, version);
                }

            }

            // Process the Skin parameters
            var parList = SkinParam.List.ToArray();
            SkinParam.Clear();

            // Assign the key of the resource-dictonary to the SkinParam Ident.
            foreach (var key in resources.Keys)
            {
				if (key is string skey && resources[key] is SkinParam res)
				{
					res.Ident = skey;

					// Exclude Empty elements
					if (res.Content == null && res.Name == null) res.Visible = false;
				}
			}

            // Retrieve special parameters
            IconBigSize = Array.Find(parList, x => x.Ident == "ParamIconBigSize");
            IconSmallSize = Array.Find(parList, x => x.Ident == "ParamIconSmallSize");

            if (IconBigSize == null)
            {
                LimeMsg.Error("ErrSkinParMiss", nameof(IconBigSize));
                return;
            }

            if (IconSmallSize == null)
            {
                LimeMsg.Error("ErrSkinParMiss", nameof(IconSmallSize));
                return;
            }


            // Remove the non-visible parameters from the list of parameters
            parList = parList.Where(x => x.Visible).ToArray();

            // Retrieve (if exists) parameters from the Configuration (settings)
            SkinParam[] configParam = null;
            if (Global.User.SkinParams != null) Global.User.SkinParams.TryGetValue(skinName, out configParam);

            // Process every paramters 
            foreach (var param in parList)
            {
                // Create, reference and retrieve the property from the config (settings)
                LimeMsg.Debug("Skin: Parameter: {0} ({1})", param.Ident, param.Type);
                if (param.Content != null)
                {
                    // If no explicit name, assign the type to the Name property
                    if (param.Name == null)
                    {
                        string name = param.Type.ToString();
                        if (name != null)
                        {
                            while (name.Contains("."))
                            {
                                name = name.Substring(name.IndexOf(".") + 1);
                            }
                            param.Name = name;
                        }
                    }

                    // Default description
                    if (param.Desc == null)
                    {
                        param.Desc = "Skin Parameter: " + param.Name;
                    }

                    // Load Setting
                    if (configParam != null && loadParam)
                    {
                        SkinParam match = Array.Find(configParam, x => x.Ident == param.Ident);
                        if (match != null)
                        {
                            string val = match.Serialize;
                            LimeMsg.Debug("Skin: Parameter: Load {0} = {1}", param.Ident, val);
                            try
                            {
                                param.Serialize = val;
                            }
                            catch
                            {
                                if (Global.User.DevMode)
                                    LimeMsg.Error("ErrSkinParValue", param.Ident, val);
                            }
                        }
                    }

                    // Monitor this property for modification
                    param.PropertyChangedWeak += SkinParamPropertyChanged;
                }
            }

            // Monitor the user-properties Scaled and EnableTypeScaled
            Global.User.PropertyChangedWeak += IconSizePropertyChanged;
            Global.Local.PropertyChangedWeak += IconSizePropertyChanged;

            // Apply the skin
            Application.Current.Resources = resources;
            Commands.MainWindow.Style = (Style)Commands.MainWindow.FindResource(typeof(Window));


            // Validate the skin
            Name = skinName;

            // Make the parameters visible (binding)
            Parameters = parList;

            // Parameter are considered as modified when reset to default
            if (!loadParam && configParam != null)
            {
                Global.User.Modified = true;
            }
        }


		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Special properties

		/// <summary>
		/// Detect change in the skin file
		/// </summary>
		private static FileSystemWatcher _Watch = null;

		/// <summary>
		/// Get or Set Size of the Small Icons
		/// </summary>
		public SkinParam IconSmallSize
        {
            get { return _IconSmallSize; }
            set
            {
                if (value != _IconSmallSize)
                {
                    _IconSmallSize = value;
                    if (value != null && value.Content is DoubleScaled)
                    {
                        if(value.Name==null) value.Name = "Small Icon Size";
                        value.Minimum = 8;
                        value.Maximum = 512;

                        Global.Root.Tree.ImgSrcSmallSize = (uint)((value.Content as DoubleScaled).Scaled * Global.Local.ScreenDPI);

                        // Action to be called back on changed property
                        _IconSmallSize.PropertyChangedWeak += IconSizePropertyChanged;
                    }
                }
            }
        }
        private SkinParam _IconSmallSize = null;


        /// <summary>
        /// Get or Set Size of the Big Icons
        /// </summary>
        public SkinParam IconBigSize
        {
            get { return _IconBigSize; }
            set
            {
                if (value != _IconBigSize)
                {
                    _IconBigSize = value;
                    if (value != null && value.Content is DoubleScaled)
                    {
                        if (value.Name == null) value.Name = "Big Icon Size";
                        value.Minimum = 16;
                        value.Maximum = 512;

                        Global.Root.Tree.ImgSrcBigSize = (uint)((value.Content as DoubleScaled).Scaled * Global.Local.ScreenDPI);

                        // Action to be called back on changed property
                        _IconBigSize.PropertyChangedWeak += IconSizePropertyChanged;
                    }
                }
            }
        }
        private SkinParam _IconBigSize = null;


        /// <summary>
        /// Set whether we should wrap the Icon in Thumbnail view or not 
        /// </summary>
        public bool IconWrapTrigger
        {
            get { return _IconWrapTrigger; }
            set
            {
                if (value != _IconWrapTrigger)
                {
                    _IconWrapTrigger = value;
                    LimeMsg.Debug("IconWrapTrigger: Changed to {0}", value);
                    OnPropertyChanged();
                }
            }
        }
        private bool _IconWrapTrigger;


        /// <summary>
        /// Keep the Button width up-to-date here
        /// </summary>
        public double IconButtonWidth
        {
            get { return _IconButtonWidth; }
            set
            {
                if (value != _IconButtonWidth)
                {
                    _IconButtonWidth = value;
                    OnPropertyChanged();
                    IconSizePropertyChanged();
                }
            }
        }
        private double _IconButtonWidth;


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Callbacks

        /// <summary>
        /// Detect changes affecting the Icon sizes to update the LimeItem Icon size
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IconSizePropertyChanged(object sender = null, PropertyChangedEventArgs e = null)
        {
            LimeMsg.Debug("Skin IconSizePropertyChanged: {0}", Name);

            Global.Root.Tree.ImgSrcSmallSize = (uint)((_IconSmallSize.Content as DoubleScaled).Scaled * Global.Local.ScreenDPI);
            Global.Root.Tree.ImgSrcBigSize = (uint)((_IconBigSize.Content as DoubleScaled).Scaled * Global.Local.ScreenDPI);

            IconWrapTrigger =  5.0 + Global.Root.Tree.ImgSrcBigSize + 2 * Global.Root.Tree.ImgSrcSmallSize < IconButtonWidth;
        }


        /// <summary>
        /// Detect a change in any of the Skin Parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkinParamPropertyChanged(object sender = null, PropertyChangedEventArgs e = null)
        {
            LimeMsg.Debug("Skin SkinParamPropertyChanged: {0} <- {1}", (sender as SkinParam)?.Ident, e.PropertyName);
            if (Global.User.Modified) return;

            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content" ||  e.PropertyName == "Item")
            {
                Global.User.Modified = true;

                // Force Save Button to activate
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();

            }
        }


		/// <summary>
		/// Delegate for <see cref="_Watch"/>
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private static void OnXamlFileChanged(object source, FileSystemEventArgs e)
		{
			if (Global.User.SkinAutoRefresh && Global.User.DevMode)
			{
				LimeMsg.Debug("Skin OnXamlFileChanged: {0} : {1}", e?.ChangeType, e.FullPath);
				if (e.ChangeType == WatcherChangeTypes.Changed)
				{
					// Thread safe call
					Global.Root.Tree?.UIElement.Dispatcher.Invoke(() => Commands.SkinReload.Execute());
				}
			}
		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Methods

		/// <summary>
		/// Retrieve the Skin Parameter matching a property identifier (key).
		/// </summary>
		/// <param name="identifier">key-name of the Skin Parameter (case-insensitive)</param>
		/// <returns>The found SkinParam, or null if not found</returns>
		public SkinParam Get(string identifier)
        {
            identifier = identifier.ToLower();

            foreach (var param in Parameters)
            {
                if (param.Ident.ToLower() == identifier)
                {
                    return param;
                }
            }

            LimeMsg.Debug("LimeProperty.Get: not found: {0}", identifier);
            return null;
        }



		/// <summary>
		/// Load the list of available skins into <see cref="Global.User.SkinList"/>.
		/// </summary>
		public void LoadSkinList()
        { 
            string path = About.SkinsPath;
            FileSystemInfo[] fInfos = null;
            try
            {
                DirectoryInfo dInfo;
                dInfo = new DirectoryInfo(path);
                fInfos = dInfo.GetFileSystemInfos();
                if (fInfos == null) LimeMsg.Error("UnableOpenDir", path);
            }
            catch
            {
                LimeMsg.Error("UnableOpenDir", path);
            }

            if (fInfos != null)
            {
                Array.Sort(fInfos, (x, y) => StringComparer.InvariantCultureIgnoreCase.Compare(x.Name, y.Name));

                var dirs = new List<string>(fInfos.Length);

                foreach (var info in fInfos)
                {
                    if ((info.Attributes & FileAttributes.Directory) != 0)
                    {
                        dirs.Add(info.Name);
                    }
                }

                Global.User.SkinList = dirs.ToArray();

                // Parameter cleanup
                if (Global.User.SkinParams != null)
                {
                    string[] keys = Global.User.SkinParams.Keys.ToArray();
                    foreach (string key in keys)
                    {
                        if (!dirs.Contains(key))
                        {
                            LimeMsg.Debug("Skin: Parameter cleanup: {0}", key);
                            Global.User.SkinParams.Remove(key);
                        }
                    }
                }


            }

        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Observable properties

        /// <summary>
        /// List of skin parameters
        /// </summary>
        public SkinParam[] Parameters
        {
            get { return _Parameters; }
            private set
            {
                if (value != _Parameters)
                {
                    _Parameters = value;
                    OnPropertyChanged();

                    if (string.IsNullOrEmpty(Name))
                    {
                        _Parameters = value;
                    }
                    else
                    {
                        if (Global.User.SkinParams == null)
                            Global.User.SkinParams = new Dictionary<string, SkinParam[]>();

                        if (!Global.User.SkinParams.ContainsKey(Name))
                            Global.User.SkinParams.Add(Name, value);
                        else
                            Global.User.SkinParams[Name] = value;
                    }
                }
            }
        }
        private SkinParam[] _Parameters;


        /// <summary>
        /// Name of the skin
        /// </summary>
        public string Name
        {
            get { return _Name; }
            private set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _Name = null;


        /// <summary>
        /// Author of the skin
        /// </summary>
        public string MetaAuthor
        {
            get { return _MetaAuthor; }
            private set
            {
                if (value != _MetaAuthor)
                {
                    _MetaAuthor = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _MetaAuthor = null;

        /// <summary>
        /// Contact (email) to the Author of the skin
        /// </summary>
        public string MetaContact
        {
            get { return _MetaContact; }
            private set
            {
                if (value != _MetaContact)
                {
                    _MetaContact = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _MetaContact = null;

        /// <summary>
        /// Website of the Author of the skin
        /// </summary>
        public string MetaWebsite
        {
            get { return _MetaWebsite; }
            private set
            {
                if (value != _MetaWebsite)
                {
                    _MetaWebsite = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _MetaWebsite = null;

        /// <summary>
        /// Description of this skin
        /// </summary>
        public string MetaDescription
        {
            get { return _MetaDescription; }
            private set
            {
                if (value != _MetaDescription)
                {
                    _MetaDescription = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _MetaDescription = null;

        #endregion


    }
}
