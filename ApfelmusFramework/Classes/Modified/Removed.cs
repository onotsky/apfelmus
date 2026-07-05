using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.Modified
{
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
