//-----------------------------------------------------------------------
// <copyright file="GeneralInformation.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System.Xml.Serialization;
using ApfelmusFramework.Classes.Allgemein;

namespace ApfelmusFramework.Classes.Information
{
    
    public class GeneralInformation
    {
        [XmlElementAttribute(ElementName = "version")]
        public string Version
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "filesystem")]
        public FileSystem FileSystem
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "system")]
        public string System
        {
            get;
            set;
        }

        public string Release
        {
            get;
            set;
        }
    }
}
