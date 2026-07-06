//-----------------------------------------------------------------------
// <copyright file="FileSystem.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Allgemein
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// Pfad-Trennzeichen des Cores (aus information.xml). Da der Core unter Windows oder Linux laufen
    /// kann, muss das GUI dessen Separator ("\\" bzw. "/") kennen, statt den lokalen anzunehmen.
    /// </summary>
    public class FileSystem
    {
        [XmlAttribute(AttributeName = "seperator")]
        public string Seperator
        {
            get;
            set;
        }
    }
}
