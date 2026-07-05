//-----------------------------------------------------------------------
// <copyright file="Modified.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace Apfelmus.Classes.Modified
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml.Serialization;
    using Apfelmus.Interfaces;

    
    public class Modified
    {
        [XmlElementAttribute(ElementName = "time")]
        public int Time
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "ids")]
        public List<Ids> Ids
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "information")]
        public Informations Information
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "download")]
        public List<Download> Download
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

        [XmlElementAttribute(ElementName = "server")]
        public Server Server
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "upload")]
        public List<Upload> Upload
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "user")]
        public List<User> User
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "search")]
        public Search Search
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "searchentry")]
        public List<SearchEntry> SearchEntry
        {
            get;
            set;
        }
    }
}
