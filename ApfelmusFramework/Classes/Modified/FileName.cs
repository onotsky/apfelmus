//-----------------------------------------------------------------------
// <copyright file="FileName.cs" company="ZSoft">
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

    public class FileName
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "user")]
        public int User
        {
            get;
            set;
        }
    }
}
