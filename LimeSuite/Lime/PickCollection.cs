/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     22-12-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Lime
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PickCollectionAttr : Attribute
    {
        public string Items { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }


        public System.Collections.IEnumerable GetItems (object source)
        {
            object ret = null;

            var mis = source.GetType().GetMember(Items);
            var mi = mis[0];

            if (mi is PropertyInfo pi)
            {
                ret = pi.GetValue(source);
            }
            else if (mi is FieldInfo fi)
            {
                ret = fi.GetValue(source);
            }

            return (System.Collections.IEnumerable)ret;
        }


        public PickCollectionAttr()
        {
        }

        public PickCollectionAttr(string items, string key = null, string name = null, string value = null)
        {
            Items = items;
            Key = key;
            Name = name;
            Value = value;
        }

    }


    /// <summary>
    /// Represents a Collection where one element (value) can be picked by name (key).
    /// This is typically represented by a Drop-down list.
    /// </summary>
    public interface IPickCollection
    {
        /// <summary>
        /// Name of the picked element
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Value of the picked element
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// List of available keys in the collection
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// User-friendly names of items in the collection
        /// </summary>
        IEnumerable<string> Names { get; }
    }


    /// <summary>
    /// Provide a base implementation of IPickCollection, with Type-conversion and Xml Serialization
    /// </summary>
    public abstract class PickCollection : StringConvertible, IPickCollection, INotifyPropertyChangedWeak
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


        #region ctors

        /// <summary>
        /// Parameterless constructor (Required for Xaml serialization)
        /// </summary>
        public PickCollection()
        {
        }

        /// <summary>
        /// Construct a PickCollection and select one element by key
        /// </summary>
        /// <param name="key">key of the element to be selected</param>
        public PickCollection(string key = null)
        {
            Key = key;
        }

        #endregion

        #region IPickCollection implementation

        /// <summary>
        /// Name of the picked element
        /// </summary>
        public abstract string Key { get; set; }

        /// <summary>
        /// Value of the picked element
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// List of available keys in the collection
        /// </summary>
        public abstract IEnumerable<string> Keys { get; }

        /// <summary>
        /// User-friendly names of items in the collection
        /// </summary>
        public abstract IEnumerable<string> Names { get; }

        #endregion

        #region StringConvertion implementation

        /// <summary>
        /// Provide a string representation of the object public properties
        /// </summary>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>string representation of the object</returns>
        public override string ToString(CultureInfo culture)
        {
            return Key;
        }

        /// <summary>
        /// Update the object from its string representation (i.e. Deserialize).
        /// </summary>
        /// <param name="source">String representing the object centent (to be)</param>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>true if the parsing of the string was successful, false otherwise</returns>
        public override bool FromString(string source, CultureInfo culture)
        {
            Key = source;
            return Key == source;
        }

        #endregion

    }


    /// <summary>
    /// Generic to build a dynamic PickCollection (collection may be different per instance and change during the object life-time).
    /// </summary>
    /// <typeparam name="T">Type of the elements in the collection</typeparam>
    /// <typeparam name="C">IEnumarable collection</typeparam>
    public class PickCollection<T> : PickCollection
	{ 

        // --------------------------------------------------------------------------------------------------
        #region PickCollection<T,C> implementation (likely to be overriden)

        /// <summary>
        /// Override this method to change the default string conversion from Value to Key
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual string ValueToKey(T value)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(string));
            if (converter.CanConvertFrom(typeof(T)))
                return (string)converter.ConvertFrom(value);
            else if (value != null)
                return value.ToString();
            else
                return null;
        }

        /// <summary>
        /// Override this method to change the default string conversion from Key to Value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual T KeyToValue(string key)
        {
            if (Items == null)
            {
                throw new InvalidOperationException("Value can't be accessed without setting Items in PickCollection");
            }

            foreach (T val in Items)
            {
                var k = ValueToKey(val);
                if (k == _Key)
                {
                    return val;
                }
            }

            throw new InvalidOperationException("Value not found in the PickCollection");
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region PickCollection implementation

        /// <summary>
        /// Name of the picked element
        /// </summary>
        public override string Key
        {
            get
            {
                return _Key;
            }

            set
            {
                if (value != _Key)
                {

                    if (Items != null)
                    {
                        bool found = false;
                        foreach (T val in Items)
                        {
                            var k = ValueToKey(val);
                            if (k == value)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new InvalidOperationException("Value not found in the PickCollection");
                        }
                    }

                    _Key = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Value");
                }
            }
        }
        private string _Key;

        /// <summary>
        /// Value of the picked element
        /// </summary>
        public override object Value
        {
            get
            {
                return KeyToValue(_Key);
            }

            set
            {
                Key = ValueToKey((T)value);
            }
        }

        /// <summary>
        /// List of available keys in the collection
        /// </summary>
        public override IEnumerable<string> Keys
        {
            get
            {
                if (Items == null) return null;

                var ret = new string[Items.Count()];
                int i = 0;
                foreach (T val in Items)
                {
                    ret[i++] = ValueToKey(val);
                }

                return ret;
            }
        }


        public override IEnumerable<string> Names
        {
            get { return null; }
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region Specific implementation

        /// <summary>
        /// Get or Set the collection content
        /// </summary>
        public virtual IEnumerable<T> Items
        {
            get
            {
                return _Items;
            }

            set
            {
                if (_Items== null || !_Items.Equals(value))
                {
                    _Items = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Keys");
                    OnPropertyChanged("Names");
                }
            }
        }
        private IEnumerable<T> _Items;

        /// <summary>
        /// Make it easy to assign to string
        /// </summary>
        /// <param name="obj"></param>
        public static implicit operator string(PickCollection<T> obj)
        {
            return obj?.ToString();
        }

        #endregion


        // --------------------------------------------------------------------------------------------------
        #region ctors

        /// <summary>
        /// Constructor
        /// </summary>
        public PickCollection(string key = null) : base(key)
        {
        }

        #endregion

    }


    /// <summary>
    /// Generic to build a static-type PickCollection (collection is shared by every instance and for all the program-life time).
    /// This can be seen as a run-time-defined Enum.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the collection</typeparam>
    /// <typeparam name="C">IEnumarable collection</typeparam>
    public abstract class PickCollectionType<T> : PickCollection<T>
    {
        /// <summary>
        /// Get or Set the type-collection
        /// </summary>
        public override IEnumerable<T> Items
        {
            get
            {
                return _Items;
            }

            set
            {
                if (!_Items.Equals(value))
                {
                    _Items = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Keys");
                    OnPropertyChanged("Names");
                }
            }
        }
        private static IEnumerable<T> _Items;


        /// <summary>
        /// Constructor
        /// </summary>
        public PickCollectionType(string key = null) : base(key)
        {
        }

    }

}
