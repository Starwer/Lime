/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     05-03-2018
* Copyright :   Sebastien Mouy © 2018  
**************************************************************************/


using Lime;
using System.Runtime.Versioning;

namespace LimeLauncher
{
    /// <summary>
    /// Provide all the ViewModel Structure as LimePropertyCollection properties.
    /// A <see cref="null"/> represents a separator.
    /// A string represents a LimeProperty from <see cref="Global.Properties"/> (command, boolean...).
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public class ConfigTree
    {
        // --------------------------------------------------------------------------------------------------
        #region Tree definition

        // -------------------------------------------
        /// <summary>
        /// Application Menu
        /// </summary>
        public LimePropertyCollection AppMenu { get; private set; } = Factory(
            "ItemAddApp",
            "ItemAdd",
			"Refresh",
			"ConfigShow",
            "ConfigSave",
            null,
			"Maximize",
			"Normal",
			"Show",
			"Hide",
			null,
            "Exit"
        );


		// -------------------------------------------
		/// <summary>
		/// Browser Commands (Menu)
		/// </summary>
		public LimePropertyCollection BrowserCommands { get; private set; } = Factory(
			"ItemAddApp",
			"ItemAdd",
			"Refresh",
			null,
			"ListView",
			"ShowInfoPane",
			"InfoEditMode",
			"ConfigVisible"
#if DEBUG
            , null,
            "CollectGarbage"
#endif
        );


		// -------------------------------------------
		/// <summary>
		/// Item Edit ToolBar
		/// </summary>
		public LimePropertyCollection EditorCommands { get; private set; } = Factory(
			"ItemSave",
			"ItemReload",
			null,
			"MetadataSearchVisible"
		);


		// -------------------------------------------
		/// <summary>
		/// Application-commands
		/// </summary>
		public LimePropertyCollection AppCommands { get; private set; } = Factory(
            "Backward",
            "Forward",
            "Home",
            "ZoomIn",
            "ZoomOut",
            "OpenUrl",
            "Refresh",
            "ConfigShow",
            "ConfigSave",
            "Show",
            "Maximize",
            "Normal",
            "Hide",
            "Exit"
        );


        // -------------------------------------------
        /// <summary>
        /// Configuration-commands
        /// </summary>
        public LimePropertyCollection ConfigurationCommands { get; private set; } = Factory(
            "ConfigSave",
            "ConfigShow",
            "ConfigClose",
            "SkinRestoreDefault",
            "SkinReload"
        );


        // -------------------------------------------
        /// <summary>
        /// Item-commands
        /// </summary>
        public LimePropertyCollection ItemCommands { get; private set; } = Factory(
            "ItemOpen",
            "ItemMenu",
            "ItemAddApp",
            "ItemAdd"
        );


        // -------------------------------------------
        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Class functions

        private static LimePropertyCollection Factory(params string[] names)
        {
            var ret = new LimePropertyCollection(null);

            foreach (var name in names)
            {
                ret.Add(name != null ? Global.Properties[name] : null);
            }

            return ret;
        }



        #endregion


    }
}
