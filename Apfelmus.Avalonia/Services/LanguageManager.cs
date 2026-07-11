using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Laufzeit-Sprachumschaltung (DE/EN/IT) analog zum WPF-Client: das zur Sprache passende
    /// ResourceDictionary (Assets/i18n/Lang.*.axaml) wird in die App-Ressourcen gemergt; alle per
    /// {DynamicResource key} gebundenen Texte aktualisieren sich dadurch sofort.
    /// </summary>
    public static class LanguageManager
    {
        private static ResourceDictionary? _current;

        public static void Apply(string code)
        {
            code = Normalize(code);
            var uri = new Uri($"avares://Apfelmus.Avalonia/Assets/i18n/Lang.{code}.axaml");
            if (AvaloniaXamlLoader.Load(uri) is not ResourceDictionary dict) return;

            var app = Application.Current;
            if (app == null) return;

            if (_current != null) app.Resources.MergedDictionaries.Remove(_current);
            app.Resources.MergedDictionaries.Add(dict);
            _current = dict;
        }

        /// <summary>
        /// Liest einen lokalisierten Text zur Laufzeit aus den App-Ressourcen (fuer im Code gesetzte
        /// Strings, die nicht per {DynamicResource} gebunden sind). Faellt auf den Schluessel zurueck.
        /// </summary>
        public static string Get(string key)
        {
            var app = Application.Current;
            if (app != null && app.TryGetResource(key, app.ActualThemeVariant, out var val) && val is string s)
                return s;
            return key;
        }

        /// <summary>Wandelt einen gespeicherten Sprach-Wert (Code oder alter Dateiname) in de/en/it.</summary>
        public static string Normalize(string? value)
        {
            string l = (value ?? string.Empty).ToLowerInvariant();
            if (l.Contains("english") || l == "en") return "en";
            if (l.Contains("italian") || l.Contains("italiano") || l == "it") return "it";
            return "de";
        }
    }
}
