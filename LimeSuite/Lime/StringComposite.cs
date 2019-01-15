/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     22-12-2016
* Copyright :   Sebastien Mouy © 2016  
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lime
{

	public interface  IStringComposite : IStringConvertible, INotifyPropertyChanged
	{
		/// <summary>
		/// String representation on the object
		/// </summary>
		string Value { get; set; }
	}



	/// <summary>
	/// Observable collection which can be represented as as string
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class StringComposite<T> : StringConvertible, IStringComposite
	{

		// --------------------------------------------------------------------------------------------------
		#region Boilerplate INotifyPropertyChanged

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

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region ctors

		/// <summary>
		/// Construct a StringComposite object from its string representation
		/// </summary>
		public StringComposite() : base()
		{
			Separator = ";";
			Cosmetic = " ";
			_Value = null;
			_Collection = null;
		}

		/// <summary>
		/// Construct a StringComposite object from its string representation
		/// </summary>
		/// <param name="value">string representation</param>
		/// <param name="separator">Separator to use</param>
		/// <param name="cosmetic">Whitespaces to be added after the separator (ignored)</param>
		public StringComposite(string value, string separator = ";", string cosmetic = " ") : base ()
		{
			Separator = separator;
			Cosmetic = cosmetic;
			_Value = value;
			_Collection = null;
		}

		/// <summary>
		/// Construct a StringComposite object from a collection
		/// </summary>
		/// <param name="collection">collection representation</param>
		/// <param name="separator">Separator to use</param>
		/// <param name="cosmetic">Whitespaces to be added after the separator (ignored)</param>
		public StringComposite(IEnumerable<T> collection, string separator = ";", string cosmetic = " ") : base()
		{
			Separator = separator;
			Cosmetic = cosmetic;
			_Value = null;
			Collection = new ObservableCollection<T>(collection);
		}


		/// <summary>
		/// Construct a StringComposite object from an ObservableCollection
		/// </summary>
		/// <param name="collection">collection representation</param>
		/// <param name="separator">Separator to use</param>
		/// <param name="cosmetic">Whitespaces to be added after the separator (ignored)</param>
		public StringComposite(ObservableCollection<T> collection, string separator = ";", string cosmetic = " ") : base()
		{
			Separator = separator;
			Cosmetic = cosmetic;
			_Value = null;
			Collection = collection;
		}



		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Properties

		public string Separator { get; private set; }
		public string Cosmetic { get; private set; }

		/// <summary>
		/// Collection representation on the object
		/// </summary>
		public ObservableCollection<T> Collection
		{
			get
			{
				return _Collection;
			}
			set
			{
				if (value != _Collection)
				{
					_Collection = value;
					_Value = null;

					if (_Collection != null) {
						_Value = "";
						var sep = Separator + Cosmetic;
						foreach (var item in _Collection)
						{
							_Value += item.ToString() + sep;
						}
					}

					OnPropertyChanged();
					OnPropertyChanged(nameof(Value));
				}
			}
		}
		private ObservableCollection<T> _Collection;


		/// <summary>
		/// String representation on the object
		/// </summary>
		public string Value
		{
			get
			{
				return _Value;
			}

			set
			{
				if (value != _Value)
				{
					_Value = value;

					// Update the collection from the string
					ObservableCollection<T> collection = null;
					if (_Value != null)
					{
						var collec = value.Split(new string[] { Separator }, StringSplitOptions.None);
						collection = _Collection ?? new ObservableCollection<T>();

						TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

						int i = 0;
						foreach (var stro in collec)
						{
							var n = i + 1;

							var str = stro.Trim();

							// Remove last empty string
							if (n == collec.Length && string.IsNullOrEmpty(str))
							{
								break;
							}

							T obj;
							if (n < collection.Count && collection[n] != null && 
								collection[n].ToString() == str)
							{
								// string matches next element
								collection.RemoveAt(i);
							}
							else if (n < collec.Length && i < collection.Count && 
								collec[n].Trim() == collection[i]?.ToString() )
							{
								// next string matches this element
								if (typeof(T) == typeof(string))
								{
									obj = (T)Convert.ChangeType(str, typeof(T));
								}
								else
								{
									obj = (T)converter.ConvertFromString(str);
								}

								collection.Insert(i, obj);
							}
							else if (i >= collection.Count || collection[i] == null || 
								collection[i].ToString() != str)
							{
								if (typeof(T) == typeof(string))
								{
									obj = (T)Convert.ChangeType(str, typeof(T));
								}
								else
								{
									obj = (T)converter.ConvertFromString(str);
								}

								if (i < collection.Count)
								{
									collection[i] = obj;
								}
								else
								{
									collection.Add(obj);
								}
							}

							i++;
						}

						// Remove remaining items
						while (i < collection.Count)
						{
							collection.RemoveAt(i);
						}
					}

					_Collection = collection;

					OnPropertyChanged();
					OnPropertyChanged(nameof(Collection));
				}
			}
		}
		private string _Value;

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region Methods

		/// <summary>
		/// Convert the collection to an array
		/// </summary>
		/// <returns>Array</returns>
		public T[] ToArray()
		{
			if (_Collection == null) return null;

			var ret = new T[_Collection.Count];
			for(int i = 0; i <  _Collection.Count; i++)
			{
				ret[i] = _Collection[i];
			}

			return ret;
		}

		#endregion


		// --------------------------------------------------------------------------------------------------
		#region StringConvertible implementation

		/// <summary>
		/// Provide a string representation of the object public properties
		/// </summary>
		/// <param name="culture">Provide a culture information</param>
		/// <returns>string representation of the object</returns>
		public override string ToString(CultureInfo culture)
		{
			return _Value;
		}


		/// <summary>
		/// Update the object from its string representation (i.e. Deserialize).
		/// </summary>
		/// <param name="source">String representing the object content (to be)</param>
		/// <param name="culture">Provide a culture information</param>
		/// <returns>true if the parsing of the string was successful, false otherwise</returns>
		public override bool FromString(string source, CultureInfo culture)
		{
			Value = source;
			return true;
		}

		#endregion

	}

}