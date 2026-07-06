//-----------------------------------------------------------------------
// <copyright file="LegacyConfig.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
//-----------------------------------------------------------------------
namespace ConfigMigrator
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Spiegelt exakt die frueher per BinaryFormatter serialisierte
    /// ApfelmusFramework.Classes.Config.Config-Klasse (gleiche private Feldnamen, da
    /// BinaryFormatter Felder ueber FormatterServices namentlich zuordnet, nicht ueber
    /// Properties). Die echte Config-Klasse ist inzwischen nicht mehr [Serializable] und
    /// nutzt XmlSerializer - dieser Nachbau existiert nur, damit alte Config.dat-Dateien
    /// ueberhaupt noch gelesen werden koennen.
    /// </summary>
    [Serializable]
    public class LegacyConfig
    {
        private string hostname;

        public string HostName
        {
            get { return hostname; }
            set { hostname = value; }
        }

        private int port;

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        private bool hideLoginWindow;

        public bool HideLoginWindow
        {
            get { return hideLoginWindow; }
            set { hideLoginWindow = value; }
        }

        private bool useCompression;

        public bool UseCompression
        {
            get { return useCompression; }
            set { useCompression = value; }
        }

        private bool protocolHandler;

        public bool ProtocolHandler
        {
            get { return protocolHandler; }
            set { protocolHandler = value; }
        }

        private int refreshRate;

        public int RefreshRate
        {
            get { return refreshRate; }
            set { refreshRate = value; }
        }

        private string languageFile;

        public string LanguageFile
        {
            get { return languageFile; }
            set { languageFile = value; }
        }

        // Erst mit dem Theme-System (2026) hinzugekommen - in Config.dat-Dateien aelterer
        // Installationen nicht vorhanden, daher optional, sonst bricht die Deserialisierung
        // fuer genau die Nutzer ab, die migrieren muessen.
        [OptionalField]
        private string theme;

        public string Theme
        {
            get { return theme; }
            set { theme = value; }
        }
    }
}
