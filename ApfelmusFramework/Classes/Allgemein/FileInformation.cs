//-----------------------------------------------------------------------
// <copyright file="FileInformation.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Allgemein
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class FileInformation
    {
        [XmlAttribute(AttributeName = "filesize")]
        public int Filesize
        {
            get;
            set;
        }
    }
}
