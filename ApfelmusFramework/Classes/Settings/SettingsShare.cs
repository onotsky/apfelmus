//-----------------------------------------------------------------------
// <copyright file="SettingsShare.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Sammlung der in den Core-Einstellungen freigegebenen Verzeichnisse (Liste von
    /// <see cref="SettingsDirectory"/>).
    /// </summary>
    public class SettingsShare
    {
        [XmlElementAttribute(ElementName = "directory")]
        public List<SettingsDirectory> Directory
        {
            get;
            set;
        }
    }
}
