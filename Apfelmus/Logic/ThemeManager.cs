using System;
using System.Linq;
using System.Windows;
using ApfelmusFramework.Classes.Serializer;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Verwaltet das aktive Farbschema (Dunkel/Hell) der Anwendung. Die Theme-Resourcedictionaries
    /// liegen als Page-Ressourcen im Apfelmus-Hauptprojekt (Resourcen\Theme.*.xaml) und werden ueber
    /// ABSOLUTE pack-URIs geladen (pack://application:,,,/Resourcen/Theme.*.xaml). Fruehere relative
    /// URIs ("..\Resourcen\...") funktionierten nur, solange diese Klasse im ApfelmusFramework-Assembly
    /// lag - nach dem Verschieben ins Apfelmus-Assembly ist der absolute pack-URI noetig.
    /// </summary>
    public static class ThemeManager
    {
        public const string Dark = "Dark";
        public const string Light = "Light";

        public static void Apply(string themeName)
        {
            string name = string.Equals(themeName, Light, StringComparison.OrdinalIgnoreCase) ? Light : Dark;

            ResourceDictionary newDictionary = new ResourceDictionary
            {
                // Absoluter pack-URI (nicht relativ), seit diese Klasse im Apfelmus-Assembly liegt.
                Source = new Uri("pack://application:,,,/Resourcen/Theme." + name + ".xaml", UriKind.Absolute)
            };

            ResourceDictionary existing = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme."));

            if (existing != null)
            {
                int index = Application.Current.Resources.MergedDictionaries.IndexOf(existing);
                Application.Current.Resources.MergedDictionaries[index] = newDictionary;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(newDictionary);
            }
        }

        public static void ApplyStartupTheme()
        {
            string theme = Dark;

            try
            {
                Config.Config config = ConfigSerializer.DeserializeFromFile();
                if (!string.IsNullOrEmpty(config.Theme))
                {
                    theme = config.Theme;
                }
            }
            catch (Exception)
            {
                // Erster Start bzw. keine gespeicherte Config - Standardtheme verwenden.
            }

            Apply(theme);
        }
    }
}
