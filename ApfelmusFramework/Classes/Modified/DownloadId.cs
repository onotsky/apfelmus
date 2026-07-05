using System;
//-----------------------------------------------------------------------
// <copyright file="DownloadId.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Modified
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class DownloadId
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName="userid")]
        public ObservableCollection<UserId> UserId
        {
            get;
            set;
        }
    }
}
