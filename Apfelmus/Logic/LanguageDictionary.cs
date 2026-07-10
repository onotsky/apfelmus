using System;
using System.Threading;
using System.Windows;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Liefert die zur Sprache passende Sprach-Ressourcen (DE/EN/IT). Dient dazu, die im GUI ueber
    /// {DynamicResource ...} gebundenen Texte je nach Kultur bzw. gespeicherter Auswahl zu setzen.
    /// </summary>
    public static class LanguageDictionary
    {
        private static ResourceDictionary dict;

        /// <summary>
        /// Waehlt das Sprach-Dictionary anhand der aktuellen Thread-Kultur (Fallback: Deutsch).
        /// </summary>
        public static ResourceDictionary GetLanguageDictionary()
        {
            dict = new ResourceDictionary();
            switch (Thread.CurrentThread.CurrentCulture.ToString())
            {
                case "de-DE":
                    dict.Source = new Uri("..\\Resourcen\\DictionaryGerman.xaml", UriKind.Relative);
                    break;
                case "en-US":
                    dict.Source = new Uri("..\\Resourcen\\DictionaryEnglish.xaml", UriKind.Relative);
                    break;
                case "it-IT":
                    dict.Source = new Uri("..\\Resourcen\\DictionaryItalian.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("..\\Resourcen\\DictionaryGerman.xaml", UriKind.Relative);
                    break;
            }

            return dict;
        }

        /// <summary>Laedt das Sprach-Dictionary explizit von dem in der Config gespeicherten Pfad.</summary>
        public static ResourceDictionary GetLanguageDictionary(string path)
        {
            dict = new ResourceDictionary();
            dict.Source = new Uri(path, UriKind.Relative);
            return dict;
        }

        /// <summary>Liefert den Standard-Pfad des zur aktuellen Kultur passenden Sprach-Dictionaries.</summary>
        public static string GetURI()
        {
            switch (Thread.CurrentThread.CurrentCulture.ToString())
            {
                case "de-DE":
                    return "..\\Resourcen\\DictionaryGerman.xaml";
                case "en-US":
                    return "..\\Resourcen\\DictionaryEnglish.xaml";
                case "it-IT":
                    return "..\\Resourcen\\DictionaryItalian.xaml";
                default:
                    return "..\\Resourcen\\DictionaryGerman.xaml";
            }
        }
    }
}
