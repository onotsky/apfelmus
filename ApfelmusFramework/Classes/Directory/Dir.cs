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
    /// <summary>
    /// Ein Verzeichnisknoten der freigegebenen Ordnerstruktur (directory.xml): Name, Pfad, Typ und
    /// die rekursiv verschachtelten Unterverzeichnisse. Bildet den Baum im "Mein Share"-Bereich.
    /// </summary>
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
