//-----------------------------------------------------------------------
// <copyright file="UploadId.cs" company="ZSoft">
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

    public class UploadId
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }
    }
}
