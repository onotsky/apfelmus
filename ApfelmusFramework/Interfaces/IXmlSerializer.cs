//-----------------------------------------------------------------------
// <copyright file="IXmlSerializer.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    /// <summary>
    /// Interface zum (De)Serializieren von XML/Objekten
    /// </summary>
    public interface IXmlSerializer
    {
        /// <summary>
        /// Zum Serialisieren
        /// </summary>
        /// <param name="obj">Objekt zu XML</param>
        /// <returns>XML-String aus Objekt</returns>
        string SerializeToXML(object obj);
        
        /// <summary>
        /// Zum Deserialisieren
        /// </summary>
        /// <param name="xmlStr">XML-String zu Objekt</param>
        /// <returns>Objekt aus XML-String</returns>
        object DeserializeToObj(string xmlStr);
    }
}
