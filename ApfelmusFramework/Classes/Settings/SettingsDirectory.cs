//-----------------------------------------------------------------------
// <copyright file="SettingsDirectory.cs" company="ZSoft">
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
    /// Ein freigegebenes Verzeichnis aus den Core-Einstellungen (settings.xml): Pfad/Name und der
    /// Freigabemodus (sharemode).
    /// </summary>
    public class SettingsDirectory
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "sharemode")]
        public string ShareMode
        {
            get;
            set;
        }
    }
}
