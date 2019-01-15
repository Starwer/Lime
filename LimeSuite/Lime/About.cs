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
using System.Text;
using System.Threading.Tasks;

namespace Lime
{

    /// <summary>
    /// Access the application meta-informations, installation, paths and versions
    /// </summary>
    public static class About
    {
        //***********************************************************************************************
        #region Types

        /// <summary>
        /// Defines the format of a Credit definition
        /// </summary>
        public class Credit
        {
            public string item { get; set; }
            public string author { get; set; }
            public string url { get; set; }

        }

        #endregion


        //***********************************************************************************************
        #region Assembly Attribute Accessors

        /// <summary>
        /// Application file-name
        /// </summary>
        public static string file
        {
            get
            {
                return Path.GetFileName(Assembly.GetEntryAssembly().CodeBase);
            }
        }

        /// <summary>
        /// Application full path
        /// </summary>
        public static string path
        {
            get
            {
                return new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
            }
        }


        /// <summary>
        /// Name of the application
        /// </summary>
        public static string name
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
                return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().CodeBase);
            }
        }


        /// <summary>
        /// Version of the application
        /// </summary>
        public static string version
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
        public static double versionNum
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
        public static string description
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
        public static string product
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
        public static string copyright
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
        public static string company
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
        public static readonly string url = "http://starwer.online.fr";

        /// <summary>
        /// Author of the application
        /// </summary>
        public static readonly string author = "Sebastien Mouy";

        #endregion



        //***********************************************************************************************
        #region Installation information

        /// <summary>
        /// Application path
        /// </summary>
        public static readonly string ApplicationPath = Assembly.GetEntryAssembly().Location;


        /// <summary>
        /// Returns the installation folder of Lime.
        /// </summary>
        public static readonly string InstallPath = Path.GetDirectoryName(ApplicationPath);

        /// <summary>
        /// Path of the Lime Configuration directory (can be a Shell-link)
        /// </summary>
        public static string ConfigPath = LimeLib.ResolvePath(Path.Combine(InstallPath, "Config"));

        /// <summary>
        /// Path of the Lime Language directory (can be a Shell-link)
        /// </summary>
        public static string LanguagePath = LimeLib.ResolvePath(Path.Combine(InstallPath, "Languages"));

        /// <summary>
        /// Path of the Lime Documentation directory (can be a Shell-link)
        /// </summary>
        public static string DocPath = LimeLib.ResolvePath(Path.Combine(InstallPath, "doc"));

        /// <summary>
        /// Path to the Lime HTML help file 
        /// </summary>
        public static string HelpPath = LimeLib.ResolvePath(Path.Combine(DocPath, "help.htm"));

        /// <summary>
        /// Path of the Lime Skins directory (can be a Shell-link)
        /// </summary>
        public static readonly string SkinsPath = LimeLib.ResolvePath(Path.Combine(About.InstallPath, "Skins"));




        #endregion


        //***********************************************************************************************
        #region Credits

        /// <summary>
        /// Credit Definition
        /// </summary>
        public static readonly List<Credit> Credits = new List<Credit>()
        {
            new Credit{ item="TagLib#", author="Aaron Bockover, Alan McGovern, Alexander Kojevnikov, Andrés G. Aragoneses, Andy Beal, Anton Drachev, Bernd Niedergesaess, Bertrand Lorentz, Colin Turner, Eamon Nerbonne, Eberhard Beilharz, Félix Velasco, Gregory S. Chudov, Guy Taylor, Helmut Wahrmann, Jakub 'Fiołek' Fijałkowski, Jeffrey Stedfast, Jeroen Asselman, John Millikin, Julien Moutte, Les De Ridder, Marek Habersack, Mike Gemünde, Patrick Dehne, Paul Lange, Ruben Vermeersch, Samuel D. Jack, Sebastien Mouy, Stephane Delcroix, Stephen Shaw, Tim Howard", url="https://github.com/mono/taglib-sharp" },
			new Credit{ item="TMDb", author="The Movie Database", url="https://www.themoviedb.org/" },
			new Credit{ item="TMDbLib", author="LordMike, Naliath", url="https://github.com/LordMike/TMDbLib" },
            new Credit{ item="Newtonsoft.Json", author="James Newton-King", url="http://www.newtonsoft.com/json" },
			new Credit{ item="WindowsThumbnailProvider", author="Daniel Peñalba", url="http://stackoverflow.com/questions/21751747/extract-thumbnail-for-any-file-in-windows" },
			new Credit{ item="DirectoryInfoEx", author="Steven Roebert", url="http://www.codeproject.com/KB/miscctrl/FileBrowser.aspx" },
            new Credit{ item="Trinet.Core.IO.Ntfs", author="Richard Deeming", url="https://github.com/hubkey/Trinet.Core.IO.Ntfs" },

            new Credit{ item="ShellLib", author="Arik Poznanski", url="http://www.codeproject.com/Articles/3590/C-does-Shell-Part" },
            new Credit{ item="IniFile", author="BLaZiNiX", url="http://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C" },
            new Credit{ item="SerializableDictionary", author="Paul Welter", url="https://weblogs.asp.net/pwelter34/444961" },
            new Credit{ item="ShellContextMenu", author="Jpmon1, Andreas Johansson", url="https://www.codeproject.com/articles/22012/explorer-shell-context-menu" },
            new Credit{ item="ShellLink", author="Mattias Sjögren", url="http://www.msjogren.net/dotnet" },
            new Credit{ item="FolderBrowserTest", author="John Dickinson", url="http://unafaltadecomprension.blogspot.nl/2013/04/browsing-for-files-and-folders-c.html" },
            new Credit{ item="WebCache", author="Scott McMaster", url="http://www.codeproject.com/Articles/13179/WebCacheTool-Manipulate-the-IE-Browser-Cache-From" },
            new Credit{ item="Win32 Interop from IRSS", author="Team Mediaportal", url="http://www.team-mediaportal.com" },
            new Credit{ item="AppxPackage", author="Simon Mourier", url="http://stackoverflow.com/questions/32122679/getting-icon-of-modern-windows-app-from-a-desktop-application" },
        };

        #endregion

    }
}
