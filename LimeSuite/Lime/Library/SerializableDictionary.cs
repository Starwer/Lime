/* 
 * Credit: Paul Welter
 * Source: https://weblogs.asp.net/pwelter34/444961
 * 
 *  05-10-2017 - Changed by Sebastien Mouy (Starwer)  
 *               Added ctors
 * 
 * For some reason, the generic Dictionary in .net 2.0 is not XML serializable.  
 * The following code snippet is a xml serializable generic dictionary.  
 * The dictionary is serialzable by implementing the IXmlSerializable interface.  
 * 
*/


using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("dictionary")]
public class SerializableDictionary<TKey, TValue>
    : Dictionary<TKey, TValue>, IXmlSerializable
{
	#region ctors

	public SerializableDictionary(SerializableDictionary<TKey, TValue> cmp) : base(cmp)
	{ }

	public SerializableDictionary(Dictionary<TKey, TValue> cmp) : base(cmp)
	{ }

	public SerializableDictionary(IEqualityComparer<TKey> cmp) : base (cmp)
    { }

    public SerializableDictionary() : base()
    { }

	#endregion

	#region IXmlSerializable Members

	public System.Xml.Schema.XmlSchema GetSchema()
	{
		return null;
	}

	public void ReadXml(System.Xml.XmlReader reader)
	{
		XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
		XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

		bool wasEmpty = reader.IsEmptyElement;
		reader.Read();

		if (wasEmpty)
			return;

		while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
		{
			reader.ReadStartElement("item");

			reader.ReadStartElement("key");
			TKey key = (TKey)keySerializer.Deserialize(reader);
			reader.ReadEndElement();

			reader.ReadStartElement("value");
			TValue value = (TValue)valueSerializer.Deserialize(reader);
			reader.ReadEndElement();

			Add(key, value);

			reader.ReadEndElement();
			reader.MoveToContent();
		}
		reader.ReadEndElement();
	}

	public void WriteXml(System.Xml.XmlWriter writer)
	{
		XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
		XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

		foreach (TKey key in Keys)
		{
			writer.WriteStartElement("item");

			writer.WriteStartElement("key");
			keySerializer.Serialize(writer, key);
			writer.WriteEndElement();

			writer.WriteStartElement("value");
			TValue value = this[key];
			valueSerializer.Serialize(writer, value);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}
	}
	#endregion

}