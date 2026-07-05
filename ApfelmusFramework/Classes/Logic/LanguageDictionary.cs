using System;
using System.Threading;
using System.Windows;

namespace ApfelmusFramework.Classes.Logic
{
    public static class LanguageDictionary
    {
        private static ResourceDictionary dict;

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

        public static ResourceDictionary GetLanguageDictionary(string path)
        {
            dict = new ResourceDictionary();
            dict.Source = new Uri(path, UriKind.Relative);
            return dict;
        }

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
