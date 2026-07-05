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
    using ApfelmusFramework.Classes.Config;

    public static class ConfigSerializer
    {
        private static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Apfelmus");
        private static string xmlFileName = Path.Combine(path, "Config.xml");
        private static XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config));

        public static void SerializeToFile(Config config)
        {
            Directory.CreateDirectory(path);
            using (FileStream fileStream = new FileStream(xmlFileName, FileMode.Create, FileAccess.Write))
            {
                xmlSerializer.Serialize(fileStream, config);
            }
        }

        public static Config DeserializeFromFile()
        {
            using (FileStream fileStream = new FileStream(xmlFileName, FileMode.Open, FileAccess.Read))
            {
                return (Config)xmlSerializer.Deserialize(fileStream);
            }
        }
    }
}
