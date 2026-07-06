//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
//-----------------------------------------------------------------------
namespace ConfigMigrator
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using ApfelmusFramework.Classes.Config;
    using ApfelmusFramework.Classes.Serializer;

    /// <summary>
    /// Einmaliges Kommandozeilen-Tool: migriert die alte, per BinaryFormatter serialisierte
    /// Config.dat auf das neue, per XmlSerializer geschriebene Config.xml-Format
    /// (siehe ConfigSerializer in ApfelmusFramework). Nach einem erfolgreichen Lauf braucht
    /// Apfelmus selbst BinaryFormatter nicht mehr - die unsichere Abhaengigkeit bleibt auf dieses
    /// separate Projekt beschraenkt.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Liest Config.dat (mit LegacyConfigBinder), uebertraegt die Werte in die aktuelle Config
        /// und schreibt sie als Config.xml; fragt vor dem Ueberschreiben nach und sichert die alte
        /// Datei als .bak. Rueckgabe: 0 = ok/nichts zu tun, 1 = Abbruch/Fehler.
        /// </summary>
        public static int Main(string[] args)
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Apfelmus");
            string datPath = Path.Combine(appDataPath, "Config.dat");
            string xmlPath = Path.Combine(appDataPath, "Config.xml");

            Console.WriteLine("Apfelmus Config-Migration (Config.dat -> Config.xml)");
            Console.WriteLine();

            if (!File.Exists(datPath))
            {
                Console.WriteLine($"Keine alte Konfiguration gefunden ({datPath}). Nichts zu tun.");
                return 0;
            }

            if (File.Exists(xmlPath))
            {
                Console.Write($"{xmlPath} existiert bereits. Ueberschreiben? [j/N]: ");
                string answer = Console.ReadLine();
                if (!string.Equals(answer, "j", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Abgebrochen.");
                    return 1;
                }
            }

            LegacyConfig legacyConfig;
            try
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter { Binder = new LegacyConfigBinder() };
                using (FileStream fileStream = new FileStream(datPath, FileMode.Open, FileAccess.Read))
                {
                    legacyConfig = (LegacyConfig)binaryFormatter.Deserialize(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Konnte {datPath} nicht lesen: {ex.Message}");
                return 1;
            }

            Config config = new Config
            {
                HostName = legacyConfig.HostName,
                Port = legacyConfig.Port,
                Password = legacyConfig.Password,
                HideLoginWindow = legacyConfig.HideLoginWindow,
                UseCompression = legacyConfig.UseCompression,
                ProtocolHandler = legacyConfig.ProtocolHandler,
                RefreshRate = legacyConfig.RefreshRate,
                LanguageFile = legacyConfig.LanguageFile,
                Theme = legacyConfig.Theme,
            };

            ConfigSerializer.SerializeToFile(config);
            Console.WriteLine($"Geschrieben: {xmlPath}");

            string backupPath = datPath + ".bak";
            File.Move(datPath, backupPath, true);
            Console.WriteLine($"Alte Datei gesichert als: {backupPath}");

            return 0;
        }
    }
}
