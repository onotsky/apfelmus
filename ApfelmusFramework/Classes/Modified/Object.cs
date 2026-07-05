using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.Modified
{
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
