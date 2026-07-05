//-----------------------------------------------------------------------
// <copyright file="BinarySerializer.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Serializer
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Serialization;
    using ApfelmusFramework.Classes.Config;

    public static class BinarySerializer
    {
        private static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Apfelmus");
        private static string datFileName = Path.Combine(path, "Config.dat");
        private static BinaryFormatter binaryFormatter = new BinaryFormatter();

        public static void SerializeToFile(Config config)
        {
            Directory.CreateDirectory(path);
            using (FileStream fileStream = new FileStream(datFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                binaryFormatter.Serialize(fileStream, config);
            }
        }

        public static Config DeserializeFromFile()
        {
            using (FileStream fileStream = new FileStream(datFileName, FileMode.Open, FileAccess.Read))
            {
                return (Config)binaryFormatter.Deserialize(fileStream);
            }
        }
    }
}
