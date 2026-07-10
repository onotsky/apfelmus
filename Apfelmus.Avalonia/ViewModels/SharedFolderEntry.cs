namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Ein aktuell freigegebenes Verzeichnis (aus den Core-Einstellungen, settings.xml/share).
    /// Dient der Anzeige der bereits freigegebenen Ordner im "Mein Share"-Tab.
    /// </summary>
    public sealed class SharedFolderEntry
    {
        public SharedFolderEntry(string path, bool withSub)
        {
            Path = path;
            WithSub = withSub;
        }

        public string Path { get; }
        public bool WithSub { get; }

        /// <summary>Anzeigetext: Pfad + Hinweis, ob Unterordner mitfreigegeben sind.</summary>
        public string Display => WithSub ? $"{Path}  (inkl. Unterordner)" : $"{Path}  (nur Ordner)";
    }
}
