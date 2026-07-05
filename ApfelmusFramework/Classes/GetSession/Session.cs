//-----------------------------------------------------------------------
// <copyright file="Session.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.GetSession
{

    public class Session
    {
        [XmlAttribute(AttributeName = "id")]
        public int id
        {
            get;
            set;
        }
    }
}
