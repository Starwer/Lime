/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Lime
{

    /// <summary>
    /// Access the application meta-informations, installation, paths and versions
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class About
    {
        //***********************************************************************************************
        #region Types

        /// <summary>
        /// Defines the format of a Credit definition
        /// </summary>
        public class Credit
        {
            public string Item { get; set; }
            public string Author { get; set; }
            public string URL { get; set; }

        }

        #endregion


        //***********************************************************************************************
        #region Assembly Attribute Accessors


        /// <summary>
        /// Name of the application
        /// </summary>
        public static string Name
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }
        }


        /// <summary>
        /// Version of the application
        /// </summary>
        public static string Version
        {
            get
            {
                var ver = Assembly.GetEntryAssembly().GetName().Version;
                return string.Format("{0}.{1:d2} (build {2})", ver.Major, ver.Minor, ver.Build);
            }
        }

        /// <summary>
        /// Version of the application, as a double
        /// </summary>
        public static double VersionNum
        {
            get
            {
                var ver = Assembly.GetEntryAssembly().GetName().Version;
                return ver.Major + ((double) ver.Minor) * 0.01;
            }
        }



        /// <summary>
        /// description of the application
        /// </summary>
        public static string Description
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        /// <summary>
        /// Product-name
        /// </summary>
        public static string Product
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        /// <summary>
        /// copyright of the application
        /// </summary>
        public static string Copyright
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        /// <summary>
        /// Company, developped and owner of the application
        /// </summary>
        public static string Company
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        /// <summary>
        /// URL of the application
        /// </summary>
        public static readonly string URL = "http://starwer.online.fr";

        /// <summary>
        /// Author of the application
        /// </summary>
        public static readonly string Author = "Sebastien Mouy";

        #endregion



        //***********************************************************************************************
        #region Installation information

        /// <summary>
        /// Returns the installation folder of Lime.
        /// </summary>
        public static readonly string InstallPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Application file
        /// </summary>
        public static readonly string ApplicationFile = System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".exe";

        /// <summary>
        /// Application path
        /// </summary>
        public static readonly string ApplicationPath = System.IO.Path.Combine(InstallPath, ApplicationFile);


        /// <summary>
        /// Path of the Lime Configuration directory (can be a Shell-link)
        /// </summary>
        public static readonly string ConfigPath = LimeLib.ResolvePath(System.IO.Path.Combine(InstallPath, "Config"));

        /// <summary>
        /// Path of the Lime Language directory (can be a Shell-link)
        /// </summary>
        public static readonly string LanguagePath = LimeLib.ResolvePath(System.IO.Path.Combine(InstallPath, "Languages"));

        /// <summary>
        /// Path of the Lime Documentation directory (can be a Shell-link)
        /// </summary>
        public static readonly string DocPath = LimeLib.ResolvePath(System.IO.Path.Combine(InstallPath, "doc"));

        /// <summary>
        /// Path to the Lime HTML help file 
        /// </summary>
        public static readonly string HelpPath = LimeLib.ResolvePath(System.IO.Path.Combine(DocPath, "help.htm"));

        /// <summary>
        /// Path of the Lime Skins directory (can be a Shell-link)
        /// </summary>
        public static readonly string SkinsPath = LimeLib.ResolvePath(System.IO.Path.Combine(About.InstallPath, "Skins"));




        #endregion


        //***********************************************************************************************
        #region Credits

        /// <summary>
        /// Credit Definition
        /// </summary>
        public static readonly List<Credit> Credits = new List<Credit>()
        {
            new Credit{ Item="TagLib#", Author="Aaron Bockover, Alan McGovern, Alexander Kojevnikov, Andrés G. Aragoneses, Andy Beal, Anton Drachev, Bernd Niedergesaess, Bertrand Lorentz, Colin Turner, Eamon Nerbonne, Eberhard Beilharz, Félix Velasco, Gregory S. Chudov, Guy Taylor, Helmut Wahrmann, Jakub 'Fiołek' Fijałkowski, Jeffrey Stedfast, Jeroen Asselman, John Millikin, Julien Moutte, Les De Ridder, Marek Habersack, Mike Gemünde, Patrick Dehne, Paul Lange, Ruben Vermeersch, Samuel D. Jack, Sebastien Mouy, Stephane Delcroix, Stephen Shaw, Tim Howard", URL="https://github.com/mono/taglib-sharp" },
			new Credit{ Item="TMDb", Author="The Movie Database", URL="https://www.themoviedb.org/" },
			new Credit{ Item="TMDbLib", Author="LordMike, Naliath", URL="https://github.com/LordMike/TMDbLib" },
            new Credit{ Item="Newtonsoft.Json", Author="James Newton-King", URL="http://www.newtonsoft.com/json" },
			new Credit{ Item="WindowsThumbnailProvider", Author="Daniel Peñalba", URL="http://stackoverflow.com/questions/21751747/extract-thumbnail-for-any-file-in-windows" },
			new Credit{ Item="DirectoryInfoEx", Author="Steven Roebert", URL="http://www.codeproject.com/KB/miscctrl/FileBrowser.aspx" },
            new Credit{ Item="Trinet.Core.IO.Ntfs", Author="Richard Deeming", URL="https://github.com/hubkey/Trinet.Core.IO.Ntfs" },

            new Credit{ Item="ShellLib", Author="Arik Poznanski", URL="http://www.codeproject.com/Articles/3590/C-does-Shell-Part" },
            new Credit{ Item="IniFile", Author="BLaZiNiX", URL="http://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C" },
            new Credit{ Item="SerializableDictionary", Author="Paul Welter", URL="https://weblogs.asp.net/pwelter34/444961" },
            new Credit{ Item="ShellContextMenu", Author="Jpmon1, Andreas Johansson", URL="https://www.codeproject.com/articles/22012/explorer-shell-context-menu" },
            new Credit{ Item="ShellLink", Author="Mattias Sjögren", URL="http://www.msjogren.net/dotnet" },
            new Credit{ Item="FolderBrowserTest", Author="John Dickinson", URL="http://unafaltadecomprension.blogspot.nl/2013/04/browsing-for-files-and-folders-c.html" },
            new Credit{ Item="WebCache", Author="Scott McMaster", URL="http://www.codeproject.com/Articles/13179/WebCacheTool-Manipulate-the-IE-Browser-Cache-From" },
            new Credit{ Item="Win32 Interop from IRSS", Author="Team Mediaportal", URL="http://www.team-mediaportal.com" },
            new Credit{ Item="AppxPackage", Author="Simon Mourier", URL="http://stackoverflow.com/questions/32122679/getting-icon-of-modern-windows-app-from-a-desktop-application" },
        };

        #endregion

    }
}
