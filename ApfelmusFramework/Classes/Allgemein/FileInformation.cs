//-----------------------------------------------------------------------
// <copyright file="FileInformation.cs" company="ZSoft">
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
    /// Dateigroesse eines Downloads aus der Partlisten-Antwort (downloadpartlist.xml). Bezugsgroesse
    /// fuer die Umrechnung der Byte-Positionen der einzelnen Parts.
    /// </summary>
    public class FileInformation
    {
        // long, weil der Core die Dateigroesse als 64-Bit-Wert liefert (Java: Long.parseLong).
        // Als int lief die XML-Deserialisierung bei Downloads > 2 GB in einen Overflow und die
        // Partliste kam kaputt/leer an.
        [XmlAttribute(AttributeName = "filesize")]
        public long Filesize
        {
            get;
            set;
        }
    }
}
