using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Liefert das zur Sprache passende Sprach-ResourceDictionary (DE/EN/IT). Die Dictionaries liegen
    /// als Page-Ressourcen im Apfelmus-Hauptprojekt (Resourcen\Dictionary*.xaml) und werden ueber
    /// ABSOLUTE pack-URIs geladen. Wichtig: fruehere relative URIs ("..\Resourcen\...") funktionierten
    /// nur, solange diese Klasse im ApfelmusFramework-Assembly lag; nach dem Verschieben ins
    /// Apfelmus-Assembly muss der absolute pack-URI verwendet werden (sonst bleiben alle
    /// {DynamicResource}-Texte leer).
    /// </summary>
    public static class LanguageDictionary
    {
        private const string Base = "pack://application:,,,/Resourcen/";

        public static ResourceDictionary GetLanguageDictionary()
        {
            return Load(Base + CultureFile());
        }

        /// <summary>Laedt das Dictionary anhand eines gespeicherten Pfads/URIs (auch alte "..\Resourcen\"-Werte).</summary>
        public static ResourceDictionary GetLanguageDictionary(string path)
        {
            string file = Path.GetFileName((path ?? string.Empty).Replace('\\', '/'));
            if (string.IsNullOrEmpty(file))
                file = CultureFile();
            return Load(Base + file);
        }

        /// <summary>Absoluter pack-URI des zur aktuellen Kultur passenden Sprach-Dictionaries.</summary>
        public static string GetURI()
        {
            return Base + CultureFile();
        }

        private static string CultureFile()
        {
            switch (Thread.CurrentThread.CurrentCulture.ToString())
            {
                case "en-US":
                    return "DictionaryEnglish.xaml";
                case "it-IT":
                    return "DictionaryItalian.xaml";
                default:
                    return "DictionaryGerman.xaml";
            }
        }

        private static ResourceDictionary Load(string uri)
        {
            return new ResourceDictionary { Source = new Uri(uri, UriKind.Absolute) };
        }
    }
}
