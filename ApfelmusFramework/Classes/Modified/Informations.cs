namespace Apfelmus.Classes.Modified
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    
    public class Informations
    {
        [XmlElementAttribute(ElementName = "information")]
        public Information Information
        {
            get;
            set;
        }

        [XmlElementAttribute(ElementName = "Networkinfo")]
        public NetworkInfo NetworkInfo
        {
            get;
            set;
        }
    }
}
