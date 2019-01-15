/**************************************************************************
* Author:       Sebastien Mouy, alias Starwer
* Contact:      starwer@laposte.net
* Website:      http://starwer.online.fr
* Creation:     10-03-2018
* Copyright :   Sebastien Mouy © 2018 
**************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Lime
{

    /// <summary>
    /// Describe a Class that can be easily converted from/to string in every culture  (i.e. Serialize).
    /// </summary>
    public interface IStringConvertible
    {
        /// <summary>
        /// Provide a comprehensive string representation of the object
        /// </summary>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>string representation of the object</returns>
        string ToString(CultureInfo culture);

        /// <summary>
        /// Update the object from its string representation (i.e. Deserialize).
        /// </summary>
        /// <param name="source">String representing the object content (to be)</param>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>true if the parsing of the string was successful, false otherwise</returns>
        bool FromString(string source, CultureInfo culture);
    }

    /// <summary>
    /// Provide a base implementation of IStringConvertion, with Type-conversion and Xml Serialization
    /// </summary>
    [XmlRoot("StringConvertible"), Serializable]
    [TypeConverter(typeof(StringConvertibleTypeConverter))]
    public abstract class StringConvertible : IStringConvertible, IXmlSerializable
    {
        public StringConvertible()
        { }

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
            string ret = null;

            string sep = culture.TextInfo.ListSeparator;
            var piSource = GetType().GetProperties();
            foreach (var pi in piSource)
            {
                object[] attributes = pi.GetCustomAttributes(true);
                bool visible = true;
                foreach (object attrib in attributes)
                {
                    XmlIgnoreAttribute xmlAttr = attrib as XmlIgnoreAttribute;
                    if (xmlAttr != null)
                    {
                        visible = false;
                        break;
                    }
                }

                // Build up list
                if (visible)
                {
                    string value;
                    var obj = pi.GetValue(this);
                    if (obj is IStringConvertible sconv)
                    {
                        value = sconv.ToString(culture);
                    }
                    else
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(obj.GetType());
                        value = converter.ConvertToString(null, culture, obj);
                    }

                    if (ret == null) ret = value; else ret += sep + value;
                }
            }

            return ret;
        }

        /// <summary>
        /// Update the object from its string representation (i.e. Deserialize).
        /// </summary>
        /// <param name="source">String representing the object content (to be)</param>
        /// <param name="culture">Provide a culture information</param>
        /// <returns>true if the parsing of the string was successful, false otherwise</returns>
        public virtual bool FromString(string source, CultureInfo culture)
        {
            string sep = culture.TextInfo.ListSeparator;

            var list = source.Split(new[] { sep }, StringSplitOptions.None);
            int idx = 0;

            var piSource = GetType().GetProperties();
            foreach (var pi in piSource)
            {
                if (idx >= list.Length) break;

                object[] attributes = pi.GetCustomAttributes(true);
                bool visible = true;
                foreach (object attrib in attributes)
                {
                    XmlIgnoreAttribute xmlAttr = attrib as XmlIgnoreAttribute;
                    if (xmlAttr != null)
                    {
                        visible = false;
                        break;
                    }
                }

                // Assign values
                if (visible)
                {
                    var value = list[idx++];
                    var obj = pi.GetValue(this);

                    if (obj is IStringConvertible sconv)
                    {
                        if (!sconv.FromString(value, culture)) return false;
                    }
                    else
                    {
                        try
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(obj.GetType());
                            var nobj = converter.ConvertFromString(null, culture, value);
                            if (!obj.Equals(nobj)) pi.SetValue(this, nobj);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        #endregion



        #region IXmlSerializable implementation

        // Xml Serialization Infrastructure

        public void WriteXml(XmlWriter writer)
        {
            var str = ToString(CultureInfo.InvariantCulture);
            writer.WriteString(str);
        }

        public void ReadXml(XmlReader reader)
        {
            string str;

            // This is not robust against nested Xml Content
            if (reader.NodeType != XmlNodeType.EndElement)
            {
                str = reader.ReadString();
            }
            else
            {
                str = "";
            }
            reader.ReadEndElement();

            FromString(str, CultureInfo.InvariantCulture);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        #endregion

    }




    /// <summary>
    /// Type Converter for StringConvertible
    /// </summary>
    public class StringConvertibleTypeConverter : TypeConverter
    {

        public StringConvertibleTypeConverter(Type type)
        {
            if (typeof(IStringConvertible).IsAssignableFrom(type))
            {
                _Type = type;
            }
            else
            {
                throw new ArgumentException("Type incompatble with IStringConvertible", type.Name);
            }
        }

        /// <summary>
        /// Source Type
        /// </summary>
        private Type _Type;


        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string) || sourceType == null);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type targetType)
        {
            return (targetType == typeof(string));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var str = value as string;
            if (str != null || value == null)
            {
                IStringConvertible obj = (IStringConvertible)Activator.CreateInstance(_Type);
                if (!obj.FromString(str, culture))
                {
                    throw new ArgumentException(string.Format("Can't convert this string to a {0}" , _Type.Name), str);
                }
                return obj;
            }

            return null;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type targetType)
        {
            if (targetType == null) throw new ArgumentNullException("targetType");

            if (value is IStringConvertible obj && targetType == typeof(string))
            {
                return obj.ToString(culture);
            }

            throw new ArgumentException(string.Format("Can't convert from {0} to this taget type", _Type.Name), targetType.Name);
        }
    }


}
