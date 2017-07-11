using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Application.Data.XML
{
    public static class XML
    {
        public static T ToObjectFromXml<T>(this string objectData)
        {
            var serializer = new XmlSerializer(typeof(T));

            using (var reader = new StringReader(objectData))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static string ToXMLString<T>(this T source)
        {
            var xsSubmit = new XmlSerializer(typeof(T));
            var sww = new StringWriter();
            using (var writer = XmlWriter.Create(sww))
            {
                xsSubmit.Serialize(writer, source);

                return sww.ToString();
            }
        }
    }
}