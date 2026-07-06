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

    /// <summary>
    /// Ein von einer Quelle angebotener Dateiname samt der User-Id, die ihn meldet. Ein Download
    /// kann bei verschiedenen Quellen unter unterschiedlichen Namen bekannt sein.
    /// </summary>
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
