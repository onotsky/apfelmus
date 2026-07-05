//-----------------------------------------------------------------------
// <copyright file="Dir.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.Directory
{
    public class Dir
    {
        #region Properties
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "path")]
        public string Path
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "isfilesystem")]
        public bool IsFileSystem
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "type")]
        public int Type
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "dir")]
        public List<Dir> Directory
        {
            get;
            set;
        }
        #endregion
    }
}
