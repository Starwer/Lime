/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     09-12-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Reflection;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Globalization;

namespace Lime
{
    // --------------------------------------------------------------------------------------------------
    #region Types
    
    /// <summary>
    /// Define an object which contains another object, and where its content notifies its container when changed. 
    /// </summary>
    public interface IMatryoshka : INotifyPropertyChanged
    {
        /// <summary>
        /// Contained object. The content notifies the container (this) on changes.
        /// </summary>
        object Content { get; set; }
    }

    /// <summary>
    /// Object presenting a weak event model in addition to the INotifyPropertyChanged
    /// </summary>
    public interface INotifyPropertyChangedWeak : INotifyPropertyChanged
    {
        /// <summary>
        /// Subscribe/unsubscribe to a Weak PropertyChanged event
        /// </summary>
        event EventHandler<PropertyChangedEventArgs> PropertyChangedWeak;
    }


    public class LimePropertyChangedEventArgs: PropertyChangedEventArgs
    {
        public string ItemPath;

        public LimePropertyChangedEventArgs(string propertyName) : base(propertyName)
        {
        }

        public LimePropertyChangedEventArgs(string propertyName, string itemPath) : base(propertyName)
        {
            ItemPath = itemPath;
        }

    }


    /// <summary>
    /// Associate additional LimeConfig attributes to a Lime configuration property
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class LimePropertyAttribute : Attribute, INotifyPropertyChangedWeak
    {
        // --------------------------------------------------------------------------------------------------
        #region Boilerplate INotifyPropertyChangedWeak

        // Boilerplate code for INotifyPropertyChanged

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
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
        #region Types

        /// <summary>
        /// Use Flags to store boolean properties. 
        /// This saves some space (one byte in total instead of one byte per propoerty).
        /// </summary>
        [Flags]
        private enum LimePropertyFlags : ushort
        {
            Visible = 0x01,
            ReadOnly = 0x02,
            ReqRestart = 0x04,
            ReqAdmin = 0x08,
            Multiline = 0x10,
            Percentage = 0x20
        }


        #endregion



        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// Boolean Properties default values
        /// </summary>
        private LimePropertyFlags Flags = LimePropertyFlags.Visible;


        /// <summary>
        /// Defines whether the object should be visible to the user (Default: Automatic)
        /// </summary>
        [XmlIgnore]
        public bool Visible
        {
            get { return Flags.HasFlag(LimePropertyFlags.Visible); }
            set
            {
                const LimePropertyFlags mask = LimePropertyFlags.Visible;
                LimePropertyFlags val = value ? mask : 0;
                if (Flags.HasFlag(LimePropertyFlags.Visible) != value)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Property can not be set
        /// </summary>
        [XmlIgnore]
        public bool ReadOnly
        {
            get { return Flags.HasFlag(LimePropertyFlags.ReadOnly); }
            set
            {
                const LimePropertyFlags mask = LimePropertyFlags.ReadOnly;
                LimePropertyFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Requires restart of application to take effect
        /// </summary>
        [XmlIgnore]
        public bool ReqRestart
        {
            get { return Flags.HasFlag(LimePropertyFlags.ReqRestart); }
            set
            {
                const LimePropertyFlags mask = LimePropertyFlags.ReqRestart;
                LimePropertyFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Requires Administrator rights to modify
        /// </summary>
        [XmlIgnore]
        public bool ReqAdmin
        {
            get { return Flags.HasFlag(LimePropertyFlags.ReqAdmin); }
            set
            {
                const LimePropertyFlags mask = LimePropertyFlags.ReqAdmin;
                LimePropertyFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Define if a string may contain several lines
        /// </summary>
        [XmlIgnore]
		public bool Multiline
        {
            get { return Flags.HasFlag(LimePropertyFlags.Multiline); }
            set
            {
                const LimePropertyFlags mask = LimePropertyFlags.Multiline;
                LimePropertyFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Define if a numeric must be shown as a percentage
        /// </summary>
        [XmlIgnore]
        public bool Percentage
        {
            get { return Flags.HasFlag(LimePropertyFlags.Percentage); }
            set
            {
                const LimePropertyFlags mask = LimePropertyFlags.Percentage;
                LimePropertyFlags val = value ? mask : 0;
                if ((Flags & mask) != val)
                {
                    Flags = (Flags & ~mask) | val;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Minimum value that a numeric value can reach
        /// </summary>
        [XmlIgnore]
        public double Minimum
        {
            get { return _Minimum; }
            set
            {
                if (value != _Minimum)
                {
                    _Minimum = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _Minimum = 0.0;

		/// <summary>
		/// Maximum value that a numeric value can reach
		/// </summary>
		[XmlIgnore]
        public double Maximum
        {
            get { return _Maximum; }
            set
            {
                if (value != _Maximum)
                {
                    _Maximum = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _Maximum = 0.0;



		/// <summary>
		/// Icon Key associated with this property
		/// </summary>
		[XmlIgnore]
		public string Icon
		{
			get { return _Icon; }
			set
			{
				if (value != _Icon)
				{
					_Icon = value;
					OnPropertyChanged();
				}
			}
		}
		private string _Icon = null;


		#endregion

		/// <summary>
		/// Parameterless Constructor
		/// </summary>
		public LimePropertyAttribute()
        {
        }

        /// <summary>
        /// Construct the class by copying from another object.
        /// </summary>
        /// <param name="attr">base attribute to copy to the new one</param>
        public LimePropertyAttribute(LimePropertyAttribute attr)
        {
            if (attr != null) LimeLib.CopyPropertyValues(attr, this);
        }

    }

    #endregion


    // --------------------------------------------------------------------------------------------------
    /// <summary>
    /// Represent a configuration property object with value, type and description, 
    /// which can reference directly an observable property.
    /// </summary>
    [Serializable]
    public class LimeProperty : LimePropertyAttribute, IMatryoshka, IStringConvertible
    {

        // --------------------------------------------------------------------------------------------------
        #region Constants 

        private const string IniLanguageSection = "Properties";
        private const string IniLanguageCommand = "Commands";

        #endregion

        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// Property info representing the Class property
        /// </summary>
        [XmlIgnore]
        public PropertyInfo PInfo { get; private set; }

        /// <summary>
        /// Parent object of the property 
        /// </summary>
        [XmlIgnore]
        public object Source { get; private set; }

        /// <summary>
        /// Get the type of the property
        /// </summary>
        [XmlIgnore]
        public Type Type { get; private set; }

        #endregion

        // --------------------------------------------------------------------------------------------------
        #region Observable Properties


        /// <summary>
        /// Get the identifier of the property as it is declared in the class
        /// </summary>
        public string Ident
        {
            get { return _Ident; }
            set
            {
                if (value != _Ident)
                {
                    _Ident = value;
                    OnPropertyChanged();
                }
            }
        }
        public string _Ident = null;


        /// <summary>
        /// Friendly (and translated) name of the property
        /// </summary>
        [XmlIgnore]
        public string Name
        {
            get { return _Name; }
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string _Name = null;


        /// <summary>
        /// Description of the property
        /// </summary>
        [XmlIgnore]
        public string Desc
        {
            get { return _Desc; }
            set
            {
                if (value != _Desc)
                {
                    _Desc = value;
                    OnPropertyChanged();
                }
            }
        }
        public string _Desc = null;


        /// <summary>
        /// Gets or set the contained property
        /// </summary>
        [XmlIgnore]
        public object Content
        {
            get
            {
                //LimeMsg.Debug("LimeProperty Content get: {0}", Ident);
                if (PInfo != null)
                {
                    return PInfo.GetValue(Source);
                }
                return Source;
            }
            set
            {
                Set(value);
            }
        }


		/// <summary>
		/// Enable to access the value of the property, even in two-way binding, as a culture-dependent string.
		/// <br>In XAML, use binding: <code>{Binding value, ValidatesOnExceptions=True}</code>
		/// </summary>
		public string Value
        {
            get
            {
                //LimeMsg.Debug("LimeProperty value get: {0}", Ident);
                return ToString();
            }

            set
            {
                LimeMsg.Debug("LimeProperty Value set: {0} = {1}", Ident, value);
                if ( ! FromString(value, CultureInfo.CurrentCulture) )
                {
                    throw new FormatException("Error converting parameter: " + Ident);
                }
            }
        }


        /// <summary>
        /// Enable to access the value of the property as a culture-independent string, for serialization
        /// </summary>
        public string Serialize
        {
            get
            {
                var ret = ToString(CultureInfo.InvariantCulture);
                LimeMsg.Debug("LimeProperty Serialize get: {0} = {1}", Ident, ret);
                return ret;
            }

            set
            {
                LimeMsg.Debug("LimeProperty Serialize set: {0} = {1}", Ident, value);
                if (!FromString(value, CultureInfo.InvariantCulture))
                {
                    throw new FormatException("Error converting parameter: " + Ident);
                }
            }
        }

        
        /// <summary>
        /// Return true if the property can be toggled
        /// </summary>
        public bool IsToggle
        {
            get
            {
				if (Type == null) return false;
                return 
                    (  Type == typeof(bool) 
                    || Type == typeof(bool?) 
                    || Type.IsEnum
                    || Content is LimeCommand cmd && cmd.IsToggle
                    );
            }
        }

		/// <summary>
		/// Get the Icon Key associated with this property
		/// </summary>
		public new string Icon
		{
			get
			{
				string ret = base.Icon;
				if (ret == null && Content is LimeCommand cmd)
				{
					ret = cmd.Icon;
				}
				return ret;
			}

			set
			{
				base.Icon = value;
			}
		}


		#endregion

		// --------------------------------------------------------------------------------------------------
		#region Methods

		/// <summary>
		/// Set the Content
		/// </summary>
		/// <param name="value">new content</param>
		/// <param name="content">notify change on "Content" if true, on "Item" if false</param>
		public void Set(object value, bool content = true)
        {
            bool changed = !Object.Equals(Content, value);
            if (!changed && Type != null) return;

            LimeMsg.Debug("LimeProperty Content set: {0} = {1}", Ident, value);

            if (PInfo != null)
            {
                PInfo.SetValue(Source, value);
            }
            else
            {
                Source = value;

                if (Type == null)
                {
                    Type = value.GetType();
                }
                else if (value != null && value.GetType() != Type)
                {
                    throw new InvalidCastException(string.Format("LimeProperty Set Content: {0} (expected {1})", value.GetType(), Type));
                }
            }


            // Bind the new object
            if (changed && value is INotifyPropertyChanged src)
            {
                //LimeMsg.Debug("LimeProperty Set: Subscribe {0} / {1} ({2})", Ident, src, src.GetType());
                src.PropertyChanged += SourcePropertyChanged;
            }

            if (content)
            {
                OnPropertyChanged("Content");
                OnPropertyChanged("Value");
                OnPropertyChanged("Serialize");
            }
            else
            {
                OnPropertyChanged("Item");
            }
        }

        /// <summary>
        /// Defines whereas a given value can be converted to the property.
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <returns>true if conversion possible</returns>
        public bool CanConvertFrom(object value)
        {
            //LimeMsg.Debug("LimeProperty CanConvertFrom: {0}", Ident);

            TypeConverter converter = TypeDescriptor.GetConverter(Type);
            if (!converter.CanConvertFrom(value.GetType())) return false;
            try
            {
                object val = converter.ConvertFrom(value);
                return val != null;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Try to toggle the value of the property
        /// </summary>
        public void Toggle()
        {
            if (Type == typeof(bool) || Type == typeof(bool?))
            {
                Content = !(Content as bool?);
            }
            else if (Type.IsEnum)
            {
                string value = Value;
                string ret = null;
                bool found = false;

                foreach (var val in Enum.GetValues(Type))
                {
                    if (ret == null) ret = val.ToString();

                    if (val.ToString() == value)
                    {
                        found = true;
                    }
                    else if (found)
                    {
                        ret = val.ToString();
                        break;
                    }
                }

                Value = ret;
            }
            else if (Content is LimeCommand cmd)
            {
                cmd.Toggle();
            }
        }

        /// <summary>
        /// Propagate the PropertyChanged event from contained object (Source) to LimeProperty Content/Value/Serialize
        /// </summary>
        /// <param name="sender">Source</param>
        /// <param name="e"></param>
        private void SourcePropertyChanged(object sender = null, PropertyChangedEventArgs e = null)
        {
            if (sender != Source) return;

            //LimeMsg.Debug("LimeProperty SourcePropertyChanged: {0} <- {1} . {2}", Ident != null ? Ident: Type.Name, sender, e.PropertyName);
            if (PInfo == null)
            {
                // Unreferenced Source
                OnPropertyChanged("Item");

                if (Ident != null && Visible)
                {
                    try
                    {
                        // TODO: retrieve instance from sender in Global
                        LimeMsg.Debug("LimeProperty SourcePropertyChanged: Item: {0}={1}  (Source={2}, Prop: {3})", 
							Ident, Value, sender, e?.PropertyName);
                    }
                    catch
                    {
                        LimeMsg.Debug("LimeProperty SourcePropertyChanged: Item: {0}", e.PropertyName);
                    }
                }

            }
            else if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == PInfo.Name)
            { 
                // Referenced Source
                if (Ident != null && Visible)
                {
                    try
                    {
                        LimeMsg.Debug("LimeProperty SourcePropertyChanged: {0}={1}  (Source={2}, Prop: {3})", 
							Ident, Value, sender, e?.PropertyName);
                    }
                    catch
                    {
                        LimeMsg.Debug("LimeProperty SourcePropertyChanged: {0}", e.PropertyName);
                    }
                }
                OnPropertyChanged("Content");
                OnPropertyChanged("Value");
                OnPropertyChanged("Serialize");
            }
        }

        #endregion


        #region IStringConvertion implementation

        /// <summary>
        /// Return the string representation in current culture
        /// </summary>
        /// <returns>string representation</returns>
        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Provide a string representation of the object public properties
        /// </summary>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>string representation of the object</returns>
        public virtual string ToString(CultureInfo culture)
        {
            object val = Content;

            if (val is IStringConvertible sconv)
            {
                return sconv.ToString(culture);
            }

            TypeConverter converter = TypeDescriptor.GetConverter(typeof(string));
            if (converter.CanConvertFrom(Type))
                return (string)converter.ConvertFrom(null, culture, val);
            else if (val != null)
                return val.ToString();
            else
                return null;
        }


        /// <summary>
        /// Update the object from its string representation (i.e. Deserialize).
        /// </summary>
        /// <param name="source">String representing the object centent (to be)</param>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>true if the parsing of the string was successful, false otherwise</returns>
        public virtual bool FromString(string source, CultureInfo culture)
        {
            bool ret = true;

            if (Source == null)
            {
                // Default content: string
                Content = source;
            }
            else
            {
                object val = null;

                if (false && Content is IStringConvertible sconv)
                {
                    var old = sconv.ToString(culture);
                    if (old != source)
                    {
                        ret = sconv.FromString(source, culture);
                        if (ret)
                        {
                            OnPropertyChanged("Content");
                            OnPropertyChanged("Value");
                            OnPropertyChanged("Serialize");
                        }
                    }
                }
                else
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(Type);
                    try
                    {
                        val = converter.ConvertFrom(null, culture, source);
                    }
                    catch { }

                    if (ret = val != null)
                    {
                        Set(val);
                    }
                }
            }

            return ret;

        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors


        /// <summary>
        /// Construct a reference to a property in a class
        /// </summary>
        /// <param name="ident">identifier of the LimeProperty</param>
        /// <param name="source">Parent object of the class where the property belongs</param>
        /// <param name="pi">PropertyInfo of the property (null if not referenced)</param>
        /// <param name="attr">Configuration attributes attached to this property</param>
        /// <param name="name">User-friendly name of the object. Automatically taken from translation by default.</param>
        /// <param name="readOnly">Define readOnly attribute</param>
        /// <param name="visible">Define visibility to the user</param>
        private void Factory(string ident, object source, PropertyInfo pi, LimePropertyAttribute attr, string name = null, bool? readOnly = null, bool? visible = null)
        {
            Source = source;
            string languageSection = IniLanguageSection;

            if (pi != null)
            {
                // Referenced source
                PInfo = pi;
                var vobj = pi.GetValue(Source);
                Type = vobj != null ? vobj.GetType() : pi.PropertyType;
                Ident = ident ?? pi.Name;
            }
            else
            {
                // Unreferenced source
                Type = source.GetType();
                Ident = ident;
            }

            // Implied attributes
            if (source is System.Windows.Input.ICommand)
            {
                ReadOnly = true;
                languageSection = IniLanguageCommand;
            }

            // Copy attribute
            if (attr != null)
            {
                if (attr.GetType() != typeof(LimePropertyAttribute))
                {
                    attr = new LimePropertyAttribute(attr);
                }
                LimeLib.CopyPropertyValues(attr, this);
            }

            // Forced attributes
            if (readOnly != null) ReadOnly = readOnly == true;
            if (visible != null) Visible = visible == true;


			// Bind the object
			if (source is INotifyPropertyChanged src)
            {
                //LimeMsg.Debug("LimeProperty Factory: Subscribe {0} / {1}", Ident, src);
                src.PropertyChanged += SourcePropertyChanged;
            }

            if (Ident == null || name != null || !Visible)
            {
                Name = name ?? ident;
                Desc = null;
            }
            else
            {
                // Retrieve properties from LimeLanguage
                Name = LimeLanguage.Translate(languageSection, Ident + ".name", Ident);
                Desc = LimeLanguage.Translate(languageSection, Ident + ".desc", Name);
            }

        }



        /// <summary>
        /// Construct a reference to a property in a class
        /// </summary>
        /// <param name="ident">identifier of the LimeProperty</param>
        /// <param name="source">Object to be referenced</param>
        /// <param name="pi">PropertyInfo of the property</param>
        /// <param name="attr">Configuration attributes attached to this property</param>
        /// <param name="name">User-friendly name of the object. Automatically taken from translation by default.</param>
        public LimeProperty(string ident, object source, PropertyInfo pi, LimePropertyAttribute attr, string name = null)
        {
            Factory(ident, source, pi, attr, name);
        }


        /// <summary>
        /// Construct a new object LimeProperty
        /// </summary>
		/// <param name="ident">identifier of the LimeProperty</param>
        /// <param name="source">object to be referenced</param>
        /// <param name="path">identifier of the property</param>
        /// <param name="reference">Reference the object if true, create independent object if false</param>
        /// <param name="name">User-friendly name of the object. Automatically taken from translation by default.</param>
        /// <param name="readOnly">Define readOnly attribute</param>
        /// <param name="visible">Define visibility to the user</param>
        public LimeProperty(string ident, object source, string path = null, string name = null, bool? readOnly = null, bool? visible = null)
        {
            PropertyInfo pi;
            LimePropertyAttribute attr = null;

            if (path != null)
            {
                pi = source.GetType().GetProperty(path);

                var attribs = pi.GetCustomAttributes(typeof(LimePropertyAttribute), true);
                if (attribs != null && attribs.Length > 0)
                {
                    attr = (LimePropertyAttribute)attribs[0];
                }

                if (ident == null) ident = path;
            }
            else
            {
                pi = null;
            }

            Factory(ident, source, pi, attr, name, readOnly, visible);
        }


		/// <summary>
		/// Construct a LimeProperty by copying another one but changing the actual referenced object
		/// </summary>
		/// <param name="ident">identifier of the LimeProperty</param>
		/// <param name="source">new source object</param>
		/// <param name="path">identifier (path) of the source to be referenced</param>
		/// <param name="copy">Source LimePropoerty to replicate</param>
		public LimeProperty(string ident, object source, string path, LimeProperty copy)
        {
            var pi = source.GetType().GetProperty(path);
            if (ident == null) ident = copy.Ident;
            Factory(ident, source, pi, copy, copy.Name);

            // Finish copying properties
            Desc = copy.Desc;
#if DEBUG
            Ident += "§";
            Name += "§";
#endif
        }


        /// <summary>
        /// Parameterless constructor to enable Xaml
        /// </summary>
        public LimeProperty()
        {
            Source = null;
            PInfo = null;
            Type = null;
            Ident = null;
            Name = null;
            Desc = null;
        }


        /// <summary>
        /// Unsubscribe the PropertyChanged source
        /// </summary>
        ~LimeProperty()
        {
            if (Source is INotifyPropertyChanged src)
            {
                //LimeMsg.Debug("LimeProperty ~LimeProperty: Unsubscribe {0} / {1}", Ident, src);
                src.PropertyChanged -= SourcePropertyChanged;
            }
        }


        #endregion
    }



}
