//-----------------------------------------------------------------------
// <copyright file="LegacyConfigBinder.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
//-----------------------------------------------------------------------
namespace ConfigMigrator
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Alte Config.dat-Dateien verweisen im Stream auf
    /// "ApfelmusFramework.Classes.Config.Config, ApfelmusFramework, Version=..." - je nachdem,
    /// mit welcher Build-Version die Datei geschrieben wurde, kann die Assembly-Version im
    /// Stream von der heute installierten ApfelmusFramework.dll abweichen (die diesen Typ
    /// ausserdem gar nicht mehr als [Serializable] fuehrt). Dieser Binder ignoriert das und
    /// lenkt jede Referenz auf den alten Config-Typ auf den lokalen Nachbau (LegacyConfig) um.
    /// </summary>
    internal sealed class LegacyConfigBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (typeName.EndsWith("Classes.Config.Config", StringComparison.Ordinal))
            {
                return typeof(LegacyConfig);
            }

            return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
        }
    }
}
