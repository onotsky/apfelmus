using System;
using System.Linq;
using System.Windows;
using ApfelmusFramework.Classes.Serializer;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Verwaltet das aktive Farbschema (Dunkel/Hell) der Anwendung. Die Theme-Resourcedictionaries
    /// liegen als lose XAML-Dateien im Apfelmus-Hauptprojekt (Resourcen\Theme.*.xaml) und werden
    /// - analog zu LanguageDictionary - relativ zum Hauptassembly aufgeloest (".." verlaesst das
    /// "ApfelmusFramework;component"-Praefix des aufrufenden Assemblies).
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
                Source = new Uri("..\\Resourcen\\Theme." + name + ".xaml", UriKind.Relative)
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
