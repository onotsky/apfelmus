//-----------------------------------------------------------------------
// <copyright file="Session.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System.Xml.Serialization;

namespace ApfelmusFramework.Classes.GetSession
{

    /// <summary>
    /// Session-Id des Cores. Sie aendert sich bei einem Core-Neustart und dient dem GUI als Marker,
    /// um zu erkennen, ob es seinen Zustand komplett neu einlesen muss.
    /// </summary>
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
