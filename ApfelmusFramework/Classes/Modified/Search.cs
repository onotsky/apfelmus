//-----------------------------------------------------------------------
// <copyright file="Search.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Modified
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Objekt das die Dateisuche repräsentiert
    /// </summary>
    public class Search
    {
        [XmlAttribute(AttributeName = "id")]
        public int id
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "searchtext")]
        public string SearchText
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "opensearches")]
        public int OpenSearches
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "foundfiles")]
        public int FoundFiles
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "sumsearches")]
        public int SumSearches
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "running")]
        public bool Running
        {
            get;
            set;
        }
    }
}
