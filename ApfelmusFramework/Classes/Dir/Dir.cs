//-----------------------------------------------------------------------
// <copyright file="Dir.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace Apfelmus.Classes.Dir
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class Dir
    {
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
    }
}
