//-----------------------------------------------------------------------
// <copyright file="UserId.cs" company="ZSoft">
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

    /// <summary>Verweis auf einen Nutzer/eine Quelle ueber seine Id im modified.xml-Delta.</summary>
    public class UserId
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }
    }
}
