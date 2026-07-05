//-----------------------------------------------------------------------
// <copyright file="Shares.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Share
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class Shares
    {
        [XmlElementAttribute(ElementName = "share")]
        public ObservableCollection<Share> Share
        {
            get;
            set;
        }
    }
}
