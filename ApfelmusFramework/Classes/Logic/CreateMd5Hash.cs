//-----------------------------------------------------------------------
// <copyright file="CreateMd5Hash.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace ApfelmusFramework.Classes.Logic
{
    
    /// <summary>
    /// Erzeugt den MD5-Hex-Digest des Klartext-Passworts. Die Core-Schnittstelle verlangt in JEDER
    /// Anfrage genau diesen MD5-Hex-Wert als password-Parameter - das ist eine Wire-Protokoll-Vorgabe
    /// des externen Cores (nicht "verbessern", sonst schlaegt der Login fehl; siehe ARCHITECTURE.md).
    /// </summary>
    public static class CreateMd5Hash
    {
        /// <summary>
        /// Gibt MD5-Hash eines Strings zurück
        /// </summary>
        /// <param name="textToHash">Passwort zum verschlüsseln</param>
        /// <returns>String verschlüsselt in MD5</returns>
        public static string GetMD5Hash(string textToHash)
        {
            // Prüfen ob Daten übergeben wurden.
            if (textToHash == null) 
            {
                return string.Empty;
            }

            // MD5 Hash aus dem String berechnen. Dazu muss der string in ein Byte[]
            // zerlegt werden. Danach muss das Resultat wieder zurück in ein string.
            using (MD5 md5 = MD5.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(textToHash);
                byte[] result = md5.ComputeHash(buffer);

                return System.BitConverter.ToString(result).Replace("-", string.Empty);
            }
        }
    }
}
