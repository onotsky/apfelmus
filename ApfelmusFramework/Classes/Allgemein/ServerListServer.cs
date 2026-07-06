using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.Allgemein
{
    /// <summary>
    /// Ein Server-Eintrag aus einer extern gepflegten Serverliste (Name, Verbindungslink, zuletzt
    /// gesehen) - nicht zu verwechseln mit Modified.Server (Laufzeitzustand eines verbundenen Servers).
    /// </summary>
    [XmlRoot(ElementName = "server")]
    public class _Server
    {
        [XmlAttribute(AttributeName = "lastseen")]
        public string Lastseen { get; set; }
        [XmlAttribute(AttributeName = "link")]
        public string Link { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Herunterladbare Serverliste. DeserializeToObj laedt die XML per HTTP von einer URL (statt aus
    /// einem uebergebenen String) und deserialisiert sie zu einer Liste von <see cref="_Server"/>,
    /// um die Serverliste zu aktualisieren/zu befuellen.
    /// </summary>
    [XmlRoot(ElementName = "serverlist")]
    public class Serverlist : Interfaces.IXmlSerializer
    {
        [XmlElement(ElementName = "server")]
        public List<_Server> Server { get; set; }

        private XmlSerializer serializer = new XmlSerializer(typeof(Serverlist));

        private static readonly HttpClient httpClient = new HttpClient();

        public object DeserializeToObj(string xmlStr)
        {
            try
            {
                byte[] responseBytes = httpClient.GetByteArrayAsync(xmlStr).GetAwaiter().GetResult();
                string data = Encoding.Default.GetString(responseBytes);

                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                {
                    return this.serializer.Deserialize(stream);
                }
            }
            catch (XmlException ex)
            {
                throw new XmlException(ex.ToString());
            }
        }

        public string SerializeToXML(object obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings() { Indent = true }))
                {
                    this.serializer.Serialize(xmlWriter, obj);
                }

                return Encoding.ASCII.GetString(stream.ToArray());
            }
        }
    }
}
