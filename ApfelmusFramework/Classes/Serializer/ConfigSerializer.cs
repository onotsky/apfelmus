//-----------------------------------------------------------------------
// <copyright file="ConfigSerializer.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Serializer
{
    using System;
    using System.IO;
    using System.Xml.Serialization;

    /// <summary>
    /// Liest/schreibt die <see cref="Config.Config"/> als %AppData%\Apfelmus\Config.xml (XmlSerializer).
    /// Loeste den frueheren BinaryFormatter/Config.dat-Ansatz ab (in .NET 9+ entfernt); die Migration
    /// alter Config.dat uebernimmt das separate Tool ConfigMigrator. Config wird bewusst voll als
    /// Config.Config qualifiziert (Namespace-Kollision, siehe ARCHITECTURE.md).
    /// </summary>
    public static class ConfigSerializer
    {
        private static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Apfelmus");
        private static string xmlFileName = Path.Combine(path, "Config.xml");
        private static XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config.Config));

        public static void SerializeToFile(Config.Config config)
        {
            Directory.CreateDirectory(path);
            using (FileStream fileStream = new FileStream(xmlFileName, FileMode.Create, FileAccess.Write))
            {
                xmlSerializer.Serialize(fileStream, config);
            }
        }

        public static Config.Config DeserializeFromFile()
        {
            using (FileStream fileStream = new FileStream(xmlFileName, FileMode.Open, FileAccess.Read))
            {
                return (Config.Config)xmlSerializer.Deserialize(fileStream);
            }
        }
    }
}
