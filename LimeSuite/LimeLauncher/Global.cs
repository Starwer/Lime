/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     16-11-2016
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/


using Lime;
using System;
using System.ComponentModel;
using System.IO;

namespace LimeLauncher
{
    /// <summary>
    /// Main entry point to the application object-structure
    /// </summary>
    public static class Global
    {
        // --------------------------------------------------------------------------------------------------
        #region Public Global Properties

        /// <summary>
        /// Root of the tree LimeItem 
        /// </summary>
        public static LimeItem Root { get; private set; }

        /// <summary>
        ///  Give access to Lime User Config properties
        /// </summary>
        public static ConfigUser User { get; private set; }

        /// <summary>
        ///  Give access to Lime Local-Machine Config properties
        /// </summary>
        public static ConfigLocal Local { get; private set; }

        /// <summary>
        ///  Give access to Lime Tree Config properties
        /// </summary>
        public static ConfigTree ConfigTree { get; private set; }

        /// <summary>
        /// Give access to the Lime Properties root
        /// </summary>
        public static LimePropertyCollection Properties { get; private set; }

        /// <summary>
        /// Give access to Lime MetaSearch singleton (Retrieve Data from Internet)
        /// </summary>
        public static LimeMetaSearch Search
		{
			get { return LimePerson.MetaSearch; }
			private set
			{
				LimePerson.MetaSearch = value;
			}
		}

#if DEBUG
		/// <summary>
		/// Debug Only: Path to the project directory
		/// </summary>
		public static string DebugProjectDir { get; set; }
#endif

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Class functions

		/// <summary>
		/// Factory: load the settings to populate the Global structure
		/// </summary>
		public static void Load()
        {

			// Debug only: retrieve the Project-directory
			#if DEBUG
			if (File.Exists("debug.cfg"))
			{
				DebugProjectDir = File.ReadAllText("debug.cfg").Trim();
			}
			#endif


            if (Root != null)
                throw new InvalidOperationException("Global.Load() can be called only once !");

            // Initialize LimeLauncher data-root
            Root = LimeItem.Load(ConfigLocal.DataPath);
            if (Root == null) Environment.Exit(10);

            // Get Icon templates
            Root.Tree.DefaultDir = new LimeItem(ConfigLocal.TemplateDirPath) { Tree = Root.Tree };
            Root.Tree.DefaultFile = new LimeItem(ConfigLocal.TemplateFilePath) { Tree = Root.Tree };

            // Initialize MetaSearch
            Search = new LimeMetaSearch();

            // Initialize Persons Local Database
            LimePerson.LocalDbPath = ConfigLocal.PersonsPath;

            // Initialize the Lime Commands
            Properties = new LimePropertyCollection(StringComparer.OrdinalIgnoreCase, typeof(Commands));
            LimeCommand.Commands = typeof(Commands);

            // Initialize LimeLauncher Settings (assume that the root is initialized properly)
            User = ConfigUser.Load();
            Properties.AddContent(User, null, false);

            Local = ConfigLocal.Load();
            Properties.AddContent(Local, null, false);

			// Initialize the Lime Config Tree
			ConfigTree = new ConfigTree();
            Properties.PropertyChanged += GlobalPropertyCollectionChanged;

        }


        private static void GlobalPropertyCollectionChanged(object sender, PropertyChangedEventArgs e)
        {
            LimeMsg.Debug("GlobalPropertyCollectionChanged: {0}", e.PropertyName);

            if (e.PropertyName == "ReqAdmin")
            {
                Properties[nameof(Commands.ConfigSave)].ReqAdmin = Properties.ReqAdmin;
            }
            else if (e.PropertyName == "Item" && e is LimePropertyChangedEventArgs ev)
            {
                try
                {
                    if (User.DevMode)
                    {
                        var prop = Properties.Get(ev.ItemPath);
                        if (prop != null && prop.Visible)
                        {
                            LimeMsg.Dev("{0}={1}", ev.ItemPath, prop.Value);
                        }
                    }
                }
                catch
                { }
            }
        }

        #endregion


    }

}
