/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     16-04-2016
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Lime
{
    /// <summary>
    /// Static Class to handle languages in Lime
    /// </summary>
    public class LimeLanguage : PickCollection
    {

        // --------------------------------------------------------------------------------------------------
        #region Attributes 

        public static string Default { get { return "en-US";  } }

        private static string path = LimeLib.ResolvePath(Path.Combine(About.LanguagePath, "en-US.ini"));
        private static IniFile ini = new IniFile(path);
        private static IniFile iniDefault = new IniFile(path);
        private static bool isIniDefault = true;

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region PickCollection implementation

        public override string Key
        {
            get { return Language; }
            set
            {
                if (value != Language)
                {
                    Language = value;
					OnPropertyChanged();
					OnPropertyChanged("Value");
				}
			}
        }

        public override object Value
        {
            get { return Key; }
            set { Key = (string)value; }
        }

        public override IEnumerable<string> Keys
        {
            get
            {
                var dict = List;
                if (dict == null) return null;
                var ret = new string[dict.Count];
                int idx = 0;
                foreach (var val in dict) ret[idx++] = val.Key;
                return ret;
            }
        }


        public override IEnumerable<string> Names
        {
            get
            {
                var dict = List;
                if (dict == null) return null;
                var ret = new string[dict.Count];
                int idx = 0;
                foreach (var val in dict) ret[idx++] = val.Value;
                return ret;
            }
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Class functions / Properties


        /// <summary>
        /// Gets or sets the language to be used for translations.
        /// </summary>
        public static string Language
        {
            get { return _Language; }
            set
            {
                if (string.IsNullOrEmpty(value)) value = Default;

                if (value != _Language)
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(value);
					CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(value);
					CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(value);

					var fpath = LimeLib.ResolvePath(Path.Combine(About.LanguagePath, value + ".ini"));
                    if (!File.Exists(fpath))
                    {
                        throw new Exception("Language not found: " + value);
                    }

                    _Language = value;
                    path = fpath;
                    ini = new IniFile(path);
                    isIniDefault = value == Default;
                }
            }
        }
        private static string _Language = Default;


        /// <summary>
        /// Load and return the list of available languages
        /// </summary>
        public static Dictionary<string, string> List
        {
            get
            {
                string path = About.LanguagePath;
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

                    var ret = new Dictionary<string, string>(fInfos.Length);
                    foreach (var info in fInfos)
                    {
                        IniFile ini = new IniFile(info.FullName);
                        string val = ini.IniReadValue("Metadata", "Name");
                        ret.Add(Path.GetFileNameWithoutExtension(info.Name), val);
                    }

                    return ret;
                }

                return null;

            }
        }



#if DEBUG
        // Avoid to display popup several times
        private static bool _DbgCompileWarning = false;
#endif


        /// <summary>
        /// Translate the key of a section to the defined language.
        /// </summary>
        /// <param name="section">Section in which the key should be retrieved.</param>
        /// <param name="key">key identifier or description to be translated</param>
        /// <param name="fallback">fallback translation to create a new entry (Debug only)</param>
        /// <returns>the translation of the key to the defined language.</returns>
        public static string Translate(string section, string key, string fallback = null)
        {
            if (key == null) return null;

            int multi = 1;

            // Retrieve language entry
            string keyval = ini.IniReadValue(section, key);
            string mkey = null;
            string mval = null;
            if (String.IsNullOrEmpty(keyval))
            {
                mkey = key + "." + multi.ToString();
                mval = ini.IniReadValue(section, mkey);
                if (!String.IsNullOrEmpty(mval))
                {
                    multi++;
                    keyval = mval;
                }
            }

#if DEBUG
            // Try to avoid crashes in Xaml designer
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)  return key;

            // Create New Entries
            if (string.IsNullOrEmpty(keyval) && fallback != null )
            {
                keyval = fallback;
                if (!isIniDefault && fallback!="") fallback = "<" + fallback + ">";
                ini.IniWriteValue(section, key, fallback);
                LimeMsg.Debug("Compile: Commands: New INI Language Command Key: {0}/{1}={2}", section, key, fallback);
                if (!_DbgCompileWarning)
                {
                    LimeMsg.Warning("CmpCreateIni");
                    _DbgCompileWarning = true;
                }

            }
#endif

            string ret;
            if (!string.IsNullOrEmpty(keyval))
            {
                ret = keyval;
            }
            else if(fallback == "")
            {
                ret = "";
            }
            else if (!isIniDefault)
            {
                // Fallback to default language if key doesn't exist
                keyval = iniDefault.IniReadValue(section, key);
                if (!String.IsNullOrEmpty(keyval)) ret = keyval;
                else ret = key;
            }
            else
            {
                ret = key;
            }

            if (multi>1)
            {
                mkey = key + "." + multi.ToString();
                while (!String.IsNullOrEmpty(mval = ini.IniReadValue(section, mkey)))
                {
                    ret += Environment.NewLine + mval;

                    multi++;
                    mkey = key + "." + multi.ToString();
                }

            }



            return ret;
        }

        #endregion

    }
}