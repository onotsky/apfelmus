//-----------------------------------------------------------------------
// <copyright file="GetObject.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace Apfelmus.Classes.GetObject
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Apfelmus.Interfaces;
    
    [XmlRootAttribute("applejuice")]
    public class GetObject : IXmlSerializer
    {
        /// <summary>
        /// Objekt zum (De)Serialisieren
        /// </summary>
        private XmlSerializer serializer = new XmlSerializer(typeof(GetObject));

        private int id;

        public GetObject(int id)
        {
            this.id = id;
        }

        [XmlElementAttribute(ElementName = "download")]
        public Download Download
        {
            get;
            set;
        }

        /// <summary>
        /// Methode zum Serialisieren
        /// </summary>
        /// <param name="obj">Objekt der Klasse</param>
        /// <returns>XML_string aus Klasse</returns>
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

        /// <summary>
        /// Methode zum Deserialisieren
        /// </summary>
        /// <param name="xmlStr">XML-String nach Objekt</param>
        /// <returns>Objekt der Klasse</returns>
        public object DeserializeToObj(string xmlStr)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(xmlStr);
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                return this.serializer.Deserialize(stream);
            }
        }
    }
}
