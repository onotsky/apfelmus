//-----------------------------------------------------------------------
// <copyright file="Download.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace Apfelmus.Classes.GetObject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class Download
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "shareid")]
        public int ShareId
        {
            get;
            set;
        }
        
        [XmlAttribute(AttributeName = "hash")]
        public string Hash
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

        [XmlAttribute(AttributeName = "status")]
        public int status
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "powerdownload")]
        public int PowerDownload
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "temporaryfilenumber")]
        public int TemporaryFileNumber
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "filename")]
        public int FileName
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "targetdirectory")]
        public int TargetDirectory
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "ready")]
        public int Ready
        {
            get;
            set;
        }
    }
}
