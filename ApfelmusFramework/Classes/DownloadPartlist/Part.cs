//-----------------------------------------------------------------------
// <copyright file="Part.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace Apfelmus.Classes.DownloadPartlist
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class Part
    {
        [XmlAttribute(AttributeName = "fromposition")]
        public int FromPosition
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "type")]
        public int type
        {
            get;
            set;
        }
    }
}
