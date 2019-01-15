/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-02-2015
* Copyright :   Sebastien Mouy © 2015  
**************************************************************************/

using System;
using System.IO;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;

namespace Lime
{
    /// <summary>
    /// Contains the base class for handling configurations of LIME (Light Integrated Multimedia Environment)
    /// and collection on LimeProperties.
    /// This exclude LimeItems, files and metadata.
    /// </summary>
    [XmlRoot("LimeConfig")]
    public abstract class LimeConfig : INotifyPropertyChangedWeak
	{

        // --------------------------------------------------------------------------------------------------
        #region Implement INotifyPropertyChanged, modified

        // Boilerplate code for INotifyPropertyChanged : Instances

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public void OnPropertyChanged(bool modified = true, [CallerMemberName] string name = null)
        {
            LimeMsg.Debug("LimeConfig OnPropertyChanged: {0}.{1} (modified={2})", this, name, modified);
            if (modified) Modified = true;
            OnPropertyChanged(new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

		// INotifyPropertyChangedWeak implementation
		public event EventHandler<PropertyChangedEventArgs> PropertyChangedWeak
		{
			add { WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(this, "PropertyChanged", value); }
			remove { WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.RemoveHandler(this, "PropertyChanged", value); }
		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Observable Properties, linked to the property

		/// <summary>
		/// Flag whether the the configuration has been modifed since last save.
		/// </summary>
		[XmlIgnore]
        public bool Modified
        {
            get { return _Modified; }
            set
            {
                if (value != _Modified)
                {
                    _Modified = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private bool _Modified = false;


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Initialize singleton class representing the Settings for LimeLauncher
        /// </summary>
        public LimeConfig()
        { }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods


        /// <summary>
        /// Force a property to be between its defined Minimum and Maximum
        /// </summary>
        /// <param name="value">Value to Coerce</param>
        /// <param name="propertyName"></param>
        public double Coerce(double value, [CallerMemberName] string propertyName = null)
        {
            if (propertyName != null)
            {
                var pi = GetType().GetProperty(propertyName);
                var attribs = pi.GetCustomAttributes(typeof(LimePropertyAttribute), true);
                if (attribs != null && attribs.Length > 0)
                {
                    var attr = (LimePropertyAttribute)attribs[0];
                    if (value < attr.Minimum) value = attr.Minimum;
                    if (value > attr.Maximum) value = attr.Maximum;
                }
            }

            return value;
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Load/Save class functions

        /// <summary>
        /// Generic function to Load the Settings of LimeLauncher from an XML file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">Path of the file/link setting representation.</param>
        /// <returns>The resulting LimeConfig (typically a singleton instance).</returns>
        public static T Load<T>(string path) where T : LimeConfig
        {
            LimeMsg.Debug("LimeConfig Load: {0}", path);

            try
            {
                XmlSerializer reader = new XmlSerializer(typeof(T));
                using (StreamReader file = new StreamReader(path))
                {
					XmlReader xread = XmlReader.Create(file); // required to preserve whitespaces
					T ret = (T)reader.Deserialize(xread);
                    ret.Modified = false;
                    return ret;
                }
            }
            catch (FileNotFoundException)
            {
                LimeMsg.Debug("Setting file not found: {0}", path);
            }
            catch (Exception ex)
            {
                LimeMsg.Error("ErrLoadFile", path);
                LimeMsg.Debug("LimeConfig Load error: {0}", ex.ToString());
            }

            return (T)Activator.CreateInstance(typeof(T));
        }


        /// <summary>
        /// Save the Settings of LimeLauncher to an XML file.
        /// </summary>
        /// <param name="path">File to save the settings to.</param>
        /// <param name="data">Instance of the data to be saved.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public static bool Save<T>(string path, T data) where T : LimeConfig
        {
            if (data == null)
            {
                LimeMsg.Error("CfgInit");
                return false;
            }

            LimeMsg.Debug("LimeConfig Save: {0}", path);

            try
            {
                XmlSerializer writer = new XmlSerializer(typeof(T));
                using (StreamWriter file = new StreamWriter(path))
                    writer.Serialize(file, data);
                data.Modified = false;
                return true;
            }
            catch
            {
                LimeMsg.Error("ErrSavFile", path);
                return false;
            }
        }


        #endregion


    }
}
