//-----------------------------------------------------------------------
// <copyright file="Informations.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Allgemein
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using ApfelmusFramework.Classes.Modified;
    using ApfelmusFramework.Interfaces;
    using ApfelmusFramework.Classes.Information;
    using ApfelmusFramework.Classes.GetSession;
    using ApfelmusFramework.Classes.Directory;
    using ApfelmusFramework.Classes.Share;
    using System.ComponentModel;
    
    

    /// <summary>
    /// Zentrales Wurzel-DTO der Core-Antwort (&lt;applejuice&gt;...). Buendelt die Teilbereiche
    /// (Downloads, Uploads, Server, User, Informationen, Session, Verzeichnisse, Share) in einem
    /// Objekt und ist zugleich der IXmlSerializer-Einstieg zum (De-)Serialisieren dieser XML.
    /// Das aktive AppleJuice; die gleichnamige Datei unter Classes/Logic ist toter, ausgeschlossener Code.
    /// </summary>
    [XmlRootAttribute("applejuice")]
    public class AppleJuice : IXmlSerializer
    {
        /// <summary>
        /// Objekt zum (De)Serialisieren
        /// </summary>
        private XmlSerializer serializer = new XmlSerializer(typeof(AppleJuice));
        #region properties
        [XmlElementAttribute(ElementName = "time")]
        public long Time
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "download")]
        public ObservableCollection<Download> Download
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "ids")]
        public Ids Ids
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "filesystem")]
        public FileSystem FileSystem
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "information")]
        public Information Information
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "networkinfo")]
        public NetworkInfo NetworkInfo
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "removed")]
        public Removed Remove
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "server")]
        public ObservableCollection<Server> Server
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "upload")]
        public ObservableCollection<Upload> Upload
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "user")]
        public ObservableCollection<User> User
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "search")]
        public ObservableCollection<Search> Search
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "searchentry")]
        public ObservableCollection<SearchEntry> SearchEntry
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "generalinformation")]
        public GeneralInformation GeneralInformation
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "session")]
        public Session Session
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "dir")]
        public ObservableCollection<Dir> Dir
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "fileinformation")]
        public FileInformation FileInformation
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "part")]
        public List<Part> Parts
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "shares")]
        public Shares Shares
        {
            get;
            set;
        }
        #endregion
        #region methods
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
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(xmlStr);
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    return this.serializer.Deserialize(stream);
                }
            }
            catch (XmlException ex)
            {
                throw new XmlException(ex.ToString());
            }
        }
        #endregion

    }
}
