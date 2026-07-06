//-----------------------------------------------------------------------
// <copyright file="Part.cs" company="ZSoft">
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
    /// Ein Abschnitt der Partliste eines Downloads: Start-Byte-Position (FromPosition) und Typ
    /// (Verfuegbarkeit/Quellenzahl-Kategorie), aus denen RenderPartList den Verfuegbarkeitsbalken
    /// zeichnet.
    /// </summary>
    public class Part
    {
        // long, weil der Core die Byte-Position als 64-Bit-Wert liefert (Java: getFromPosition()).
        // Als int kippten die Positionen bei Downloads > 2 GB in den Overflow -> falsche/kaputte
        // Partliste. Die Render-Logik in MainWindow.RenderPartList rechnet ohnehin mit long.
        [XmlAttribute(AttributeName = "fromposition")]
        public long FromPosition
        {
            get;
            set;
        }

        [XmlAttribute(AttributeName = "type")]
        public int type
        {
            get;
            set;
        }
    }
}
