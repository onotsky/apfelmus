using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.Modified
{
    /// <summary>
    /// Liste der im letzten modified.xml-Delta entfernten Objekte (&lt;removed&gt;&lt;object id=".."/&gt;...).
    /// Anhand dieser Ids raeumt das GUI die entsprechenden Eintraege aus seinen Collections.
    /// </summary>
    public class Removed
    {
        [XmlElementAttribute(ElementName = "object")]
        public ObservableCollection<ApfelmusFramework.Classes.Modified.Object> Object
        {
            get;
            set;
        }
    }
}
