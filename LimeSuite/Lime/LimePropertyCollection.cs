/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-09-2017
* Copyright :   Sebastien Mouy © 2017  
**************************************************************************/

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Versioning;

namespace Lime
{
    /// <summary>
    /// Represent a ViewModel collection of LimeProperty, where any modification in a child is progagated up the tree.
    /// It also allow to change any logical (Model) object (Class, IEnumerable, Collection...) to a ViewModel representation. 
    /// The special pseudo PropertyChanged "Item" indicates a change inside an Item of any collection down the tree. 
    /// A Subscriber handling LimePropertyChangedEventArgs can retrieve the path (in the tree) of the object which changed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LimePropertyCollection : ObservableCollection<LimeProperty>, INotifyPropertyChangedWeak

    {
        // --------------------------------------------------------------------------------------------------
        #region Constants & Static properties

        /// <summary>
        /// Section for language access
        /// </summary>
        private const string IniLanguageSection = "Types";

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Property change handling

        // Inherit PropertyChanged trick
        public new event PropertyChangedEventHandler PropertyChanged
        {
            add { base.PropertyChanged += value; }
            remove { base.PropertyChanged -= value; }
        }

        // INotifyPropertyChangedWeak implementation
        public event EventHandler<PropertyChangedEventArgs> PropertyChangedWeak
        {
            add { WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(this, "PropertyChanged", value); }
            remove { WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.RemoveHandler(this, "PropertyChanged", value); }
        }


        protected void OnPropertyChanged(bool modified = true, [CallerMemberName] string propertyName = null, string itemPath = null)
        {
            //LimeMsg.Debug("LimePropertyCollections OnPropertyChanged: {0} ({1}) : {2}", itemPath ?? (Source as LimeProperty)?.Ident, Count, propertyName);

            if (modified && propertyName != "Modified")
            {
				//if (Modified != true && this is LimeMetadata meta && meta.Title == "Avatar")
				//{
				//	LimeMsg.Debug("TODO");
				//}
				Modified = true;
            }

            OnPropertyChanged(new LimePropertyChangedEventArgs(propertyName, itemPath));
        }


        /// <summary>
        /// Monitor property changes in the items of the collection
        /// </summary>
        /// <param name="sender">Should be a LimeProperty</param>
        /// <param name="e"></param>
        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Content")
                && sender is LimeProperty dest && Source is LimeProperty src)
            {
                if (src != dest && src.Content is IList collec)
                {
                    int i = 0;
                    foreach (var item in this)
                    {
                        if (i >= collec.Count) break;
                        if (item == sender)
                        {
                            if (!(collec[i] is LimeProperty))
                            {
                                //LimeMsg.Debug("LimePropertyCollection PropertyChangedCallback: Update Source at {0}: {1}", i, item.Name);
                                collec[i] = item.Content;
                                src.Set(collec, false);
                                break;
                            }
                        }
                        i++;
                    }
                }
            }

            // Avoid Value & Serialize as these gets updated always after a Content, so this is useless callback
            if (sender is LimeProperty prop && e.PropertyName != "Value" && e.PropertyName != "Serialize")
            {
#if DEBUG
                //if (prop.Ident != "OnTop" && prop.Ident != "TaskHandle")
                //{
                //    // Update on a Lime Property contained by this LimePropertyCollection
                //    LimeMsg.Debug("LimePropertyCollections ItemPropertyChanged: [{0}] {1} = {2}", prop.Name, e.PropertyName, prop.Type);
                //}
#endif
                // Bubble up some properties on content change
                if (e.PropertyName == "Content")
                {
                    if (prop.ReqRestart) ReqRestart = true;
                    if (prop.ReqAdmin) ReqAdmin = true;
                }

                // Recompose LimeProperty path
                string path = prop.Ident;
                if (e is LimePropertyChangedEventArgs ev)
                {
                    if (ev.ItemPath != null) path += "." + ev.ItemPath;
                }
                
                // Notify Item change on this collection
                OnPropertyChanged(true, "Item", path);

                // Relay the change event to the Source when Collection is generated from non observable collection
                if (Source is LimeProperty sprop && sprop.Content != this)
                {
                    sprop.OnPropertyChanged("Item");
                }

            }
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Properties

        /// <summary>
        /// Source which was used to generate the Collection
        /// </summary>
        public object Source { get; private set; }


        public StringComparer StringComparer { get; private set; }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Observable Properties

        /// <summary>
        /// Flag whether the an object inside this collection has been modified.
        /// </summary>
        [XmlIgnore]
        public bool Modified
        {
            get { return _Modified == true; }
            set
            {
                if (value != _Modified && _Modified != null)
                {
                    _Modified = value;
                    OnPropertyChanged(false);
                }
            }
        }
        protected bool? _Modified = false;


        /// <summary>
        /// Flag whether applying the mofifications requires a restart of the application.
        /// </summary>
        [XmlIgnore]
        public bool ReqRestart
        {
            get { return _ReqRestart; }
            set
            {
                if (value != _ReqRestart)
                {
                    _ReqRestart = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private bool _ReqRestart = false;



        /// <summary>
        /// Flag whether applying the mofifications requires Administrator rights.
        /// </summary>
        [XmlIgnore]
        public bool ReqAdmin
        {
            get { return _ReqAdmin; }
            set
            {
                if (value != _ReqAdmin)
                {
                    _ReqAdmin = value;
                    OnPropertyChanged(false);
                }
            }
        }
        private bool _ReqAdmin = false;


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Create a Collection to contain LimeProperty from any source object
        /// </summary>
        /// <param name="mode">StringComparer defining the Sorting mode by LimeProperty identifiers (null: declaration order)</param>
        /// <param name="source">Source collection or class to make the LimeProperty from</param>
        /// <param name="attr">LimePropertyAttribute to apply to the items in the collection</param>
        /// <param name="all">Take all public objects if true, only Xml or LimePropertyAttribute visible elements is false</param>
        public LimePropertyCollection(StringComparer mode, object source = null, LimePropertyAttribute attr = null, bool all = true)
        {
            StringComparer = mode;
            Source = source;
            if (source == null) return;

            // Unpack LimePropertyAttribute
            if (attr == null && source is LimePropertyAttribute psrc)
            {
                if (attr == null) attr = psrc;

            }

            // Unpack IMatryoshka, LimeProperty
            if (source is IMatryoshka matr)
            {
                source = matr.Content;
                if (source == null) return;
            }

            // Force the LimePropertyAttribute to be *only* a LimePropertyAttribute (otherwises it will copy all the object)
            if (attr != null && attr.GetType() != typeof(LimePropertyAttribute))
            {
                attr = new LimePropertyAttribute(attr);
            }

            if (source is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    var prop = item as LimeProperty;
                    if (prop == null)
                    {
                        prop = new LimeProperty(null, item, null, attr);
                    }
                    Add(prop);
                }
            }
            else
            {
                AddContent(source, attr, all);
            }

        }

        /// <summary>
        /// Unsubscribe all PropertyChanged
        /// </summary>
        ~LimePropertyCollection()
        {
            foreach (var prop in this)
            {
                if (prop != null) prop.PropertyChanged -= ItemPropertyChanged;
            }
        }

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ObservableCollection implementation: subscribe/unsubscribe to the properties

		/// <summary>
		/// Insert a LimeProperty to the collection
		/// </summary>
		/// <param name="InsertItem">Index at which the property should be added</param>
		/// <param name="prop">LimeProperty to be added</param>
		protected override void InsertItem(int index, LimeProperty prop)
        {
            if (StringComparer != null) index = FindBestIndex(prop?.Ident, index);
            if (prop != null) prop.PropertyChanged += ItemPropertyChanged;
            base.InsertItem(index, prop);
        }

        /// <summary>
        /// Replace a LimeProperty at a certain index
        /// </summary>
        /// <param name="index">Index at which the property should be modified</param>
        /// <param name="prop">LimeProperty to be inserted</param>
        protected override void SetItem(int index, LimeProperty prop)
        {
            if (StringComparer != null) index = FindBestIndex(prop?.Ident, index);
            var old = this[index];
            if (old != null) old.PropertyChanged -= ItemPropertyChanged;
            if (prop != null) prop.PropertyChanged += ItemPropertyChanged;

            base.InsertItem(index, prop);
        }

        /// <summary>
        /// Remove a LimeProperty at a certain index
        /// </summary>
        /// <param name="index">Index at which the property should be removed</param>
        protected override void RemoveItem(int index)
        {
            var prop = this[index];
            if (prop != null) prop.PropertyChanged -= ItemPropertyChanged;
            base.RemoveItem(index);
        }

        /// <summary>
        /// Clear all the contained LimeProperties
        /// </summary>
        protected override void ClearItems()
        {
            foreach (var prop in this)
            {
                if (prop != null) prop.PropertyChanged -= ItemPropertyChanged;
            }

            base.ClearItems();
        }


        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Methods


        /// <summary>
        /// Find the best index to insert an element or retrieve the first closest match on a sorted collection.
        /// This is implements a binary search algorithm, which ensure a complexity O(log(n)).
        /// </summary>
        /// <param name="key">Key to retrieve</param>
        /// <param name="index">Proposed index for inserting a new key (-1 to retrieve first closest match)</param>
        /// <returns>The index of the closest match</returns>
        private int FindBestIndex(string key, int index = -1)
        {
            if (index >= 0 && index < Count && StringComparer.Equals(this[index]?.Ident, key))
            {
                return index; // accepted proposed index
            }

            // Apply binary search

            int idx = 0; // low boundary
            int odx = Count; // high boundary
            int size = Count >> 1; // median relative position

            while (idx != odx)
            {
                int mdx = idx + size;
                var ident = this[mdx]?.Ident;
                int comp = StringComparer.Compare(key, ident);
                if (comp < 0 || comp == 0 && index < mdx)
                {
                    odx = mdx; // precedes
                }
                else
                {
                    idx = mdx + 1; // follows
                }

                size = (odx - idx) >> 1;
            }

            return idx;
        }



        /// <summary>
        /// Add the content of any object to the collection.
        /// </summary>
        /// <param name="source">source object, class, collection...</param>
        /// <param name="attr">Default LimePropertyAttribute to apply to the items in the collection</param>
        /// <param name="all">Take all public objects if true, only Xml or LimePropertyAttribute visible elements is false</param>
        public void AddContent(object source, LimePropertyAttribute attr = null, bool all = true)
        {
            var field = source as Type;

            MemberInfo[] miSource;
            if (field != null)
            {
                miSource = field.GetFields();
            }
            else
            {
                miSource = source.GetType().GetProperties();
            }
            
            // Populate list
            foreach (var mi in miSource)
            {
                object[] attributes = mi.GetCustomAttributes(true);
                bool visible = all;
                LimePropertyAttribute cfgAttr = null;
                foreach (object attrib in attributes)
                {
                    XmlElementAttribute xmlAttr = attrib as XmlElementAttribute;
                    if (xmlAttr != null) visible = true;

                    cfgAttr = attrib as LimePropertyAttribute;
                    if (cfgAttr != null)
                    {
                        visible = cfgAttr.Visible;
                        break;
                    }

                }

                if (cfgAttr == null) cfgAttr = attr;
                if (visible)
                {
                    if (all && (attr == null || attr.Visible) && mi is PropertyInfo pi)
                    {
                        string ident = string.Format("{0}.{1}", source.GetType().Name, mi.Name);
                        string name = LimeLanguage.Translate(IniLanguageSection, ident + ".name", mi.Name);
                        var prop = new LimeProperty(null, source, pi, cfgAttr, name);
                        prop.Desc = LimeLanguage.Translate(IniLanguageSection, ident + ".desc", name);
                        Add(prop);
                    }
                    else if (mi is PropertyInfo pi2)
                    {
                        Add(new LimeProperty(null, source, pi2, cfgAttr));
                    }
                    else if (mi is FieldInfo fi)
                    {
                        var obj = fi.GetValue(field);
                        Add(new LimeProperty(mi.Name, obj));
                    }
                    else
                    {
                        throw new Exception("LimePropertyCollection AddContent: Unuspported type");
                    }
                }
            }
        }


        /// <summary>
        /// Retrieve the Lime Property matching a property identifier key.
        /// </summary>
        /// <param name="key">identifier of the property (case-insensitive)</param>
        /// <param name="property">LimeProperty found, or null if not found</param>
        /// <returns>true if the LimeProperty exists (found)</returns>
        public bool TryGet(string key, out LimeProperty property)
        {
            if (StringComparer != null && Count>0)
            {
                var index = FindBestIndex(key);
                var prop = index < Count ? this[index] : null;
                var ret = StringComparer.Equals(prop?.Ident, key);
                property = ret ? prop : null;
                return ret;
            }

            foreach (var prop in this)
            {
                if (String.Equals(prop.Ident, key, StringComparison.OrdinalIgnoreCase))
                {
                    property = prop;
                    return true;
                }
            }

            property = null;
            return false;
        }


        /// <summary>
        /// Retrieve the Lime Property matching a property identifier key.
        /// </summary>
        /// <param name="key">identifier of the property (case-insensitive)</param>
        /// <returns>LimeProperty found, or null if not found</returns>
        public LimeProperty Get(string key)
        {
            TryGet(key, out var ret);
            return ret;
        }


        /// <summary>
        /// Gets the Lime Property associated with the specified key 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LimeProperty this[string key]
        {
            get
            {
                if (!TryGet(key, out var ret))
                {
                    LimeMsg.Debug("LimePropertyCollection [key]: key not found: {0}", key);
                    throw new InvalidProgramException(string.Format("LimePropertyCollection [key]: key not found: {0}", key));
                }
                return ret;
            }
        }


        /// <summary>
        /// Add a new property to the collection and bind it to an object.
        /// </summary>
        /// <param name="key">Name of the property</param>
        /// <param name="source">object containing the property to bind</param>
        /// <param name="path">property identifier to bind</param>
        /// <param name="readOnly">Define if the the property in read-only</param>
        /// <param name="visible">Define if the the property is visible to the user</param>
        /// <param name="multiline">Define if the the property is a multiline string</param>
        /// <param name="allowEmpty">Define if a numeric Is considered Empty when zero-ed</param>
        /// <returns>The created LimeProperty</returns>
        public LimeProperty Add(string key, object source, string path, bool? readOnly = null, bool? visible = null, bool? multiline = null, bool? allowEmpty = null)
		{
			//LimeMsg.Debug("LimePropertyCollection: Add: {0}", key);

			var prop = new LimeProperty(key, source, path, null, readOnly, visible);
            if (multiline != null) prop.Multiline = multiline == true;
            if (allowEmpty != null) prop.AllowEmpty = allowEmpty == true;
            Add(prop);
            return prop;
        }


        /// <summary>
        /// Add a new property to the collection based on a Key/value pair.
        /// </summary>
        /// <param name="key">Name of the property</param>
        /// <param name="value">Value of the property</param>
        /// <param name="readOnly">Define if the the property is read-only</param>
        /// <param name="visible">Define if the the property is visible to the user</param>
        /// <returns>The created LimeProperty</returns>
        public LimeProperty Add(string key, object value, bool readOnly = false, bool visible = true, bool? allowEmpty = null)
		{
            if (value is LimeProperty)
                throw new InvalidOperationException("LimePropertyCollection Add new");

			if (value == null) value = "";
			var prop = new LimeProperty(key, value, null, null, readOnly, visible);
            if (allowEmpty != null) prop.AllowEmpty = allowEmpty == true;
			Add(prop);
            return prop;
        }


        /// <summary>
        /// Add a Label
        /// </summary>
        /// <param name="key">Label Key</param>
        /// <returns>The created LimeProperty</returns>
        public LimeProperty Add(string key)
        {
            const string LanguageSection = "Translator";

            var prop = new LimeProperty();
            prop.Ident = key;
            prop.Name = LimeLanguage.Translate(LanguageSection, key, key);
            Add(prop);
            return prop;
        }


        #endregion



    }
}
