//-----------------------------------------------------------------------
// <copyright file="SearchEntry.cs" company="ZSoft">
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

    public class SearchEntry
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "searchid")]
        public int SearchId
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "checksum")]
        public string Checksum
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "size")]
        public int Size
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "filename")]
        public FileName FileName
        {
            get;
            set;
        }

        public int SearchColor
        {
            get;
            set;
        }
    }
}
