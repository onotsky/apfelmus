//-----------------------------------------------------------------------
// <copyright file="Settings.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using ApfelmusFramework.Interfaces;

    /// <summary>
    /// Klasse zum speichern der Settings
    /// </summary>
    [XmlRootAttribute("settings")]
    public class Settings : IXmlSerializer
    {
        /// <summary>
        /// Objekt zum (De)Serialisieren
        /// </summary>
        private XmlSerializer serializer = new XmlSerializer(typeof(Settings));

        /// <summary>
        /// Gets or sets Nick
        /// </summary>
        [XmlElementAttribute(ElementName = "nick")]
        public string Nick
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Port
        /// </summary>
        [XmlElementAttribute(ElementName = "port")]
        public int Port
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets XmlPort
        /// </summary>
        [XmlElementAttribute(ElementName = "xmlport")]
        public int XmlPort
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets AutoConnect
        /// </summary>
        [XmlElementAttribute(ElementName = "autoconnect")]
        public bool AutoConnect
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets MaxUpload
        /// </summary>
        [XmlElementAttribute(ElementName = "maxupload")]
        public int MaxUpload
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets MaxDownload
        /// </summary>
        [XmlElementAttribute(ElementName = "maxdownload")]
        public int MaxDownload
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets MaxConnections
        /// </summary>
        [XmlElementAttribute(ElementName = "maxconnections")]
        public int MaxConnections
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets MaxSourcesPerFile
        /// </summary>
        [XmlElementAttribute(ElementName = "maxsourcesperfile")]
        public int MaxSourcesPerFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets SpeedPerSlot
        /// </summary>
        [XmlElementAttribute(ElementName = "speedperslot")]
        public int SpeedPerSlot
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets MaxNewConnectiosPerTurn
        /// </summary>
        [XmlElementAttribute(ElementName = "maxnewconnectionsperturn")]
        public int MaxNewConnectionsPerTurn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets IncomingDirectory
        /// </summary>
        [XmlElementAttribute(ElementName = "incomingdirectory")]
        public string IncomingDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets TemporaryDirectory
        /// </summary>
        [XmlElementAttribute(ElementName = "temporarydirectory")]
        public string TemporaryDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Share
        /// </summary>
        [XmlElementAttribute(ElementName = "share")]
        public SettingsShare share
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
