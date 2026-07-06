using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.Modified
{
    /// <summary>
    /// Minimaler Traeger einer Objekt-Id im modified.xml-Delta (z.B. innerhalb von &lt;removed&gt;,
    /// um zu melden, welcher Eintrag entfernt wurde).
    /// </summary>
    public class Object
    {
        [XmlAttribute(AttributeName = "id")]
        public int Id
        {
            get;
            set;
        }
    }
}
