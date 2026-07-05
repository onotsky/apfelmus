//-----------------------------------------------------------------------
// <copyright file="Ids.cs" company="ZSoft">
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

    public class Ids
    {
        [XmlElementAttribute(ElementName = "serverid")]
        public ObservableCollection<ServerId> ServerId
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "uploadid")]
        public ObservableCollection<UploadId> UploadId
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "downloadid")]
        public ObservableCollection<DownloadId> DownloadId
        {
            get;
            set;
        }
    }
}
