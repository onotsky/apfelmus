using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Avalonia.Threading;

namespace Apfelmus.Avalonia.Services
{
    public enum LogLevel { Info, Warning, Error }

    /// <summary>Ein Eintrag im GUI-Ereignisprotokoll (Zeitpunkt, Stufe, Text).</summary>
    public sealed class LogEntry
    {
        public LogEntry(DateTime time, LogLevel level, string message)
        {
            Time = time;
            Level = level;
            Message = message;
        }

        public DateTime Time { get; }
        public LogLevel Level { get; }
        public string Message { get; }

        public string TimeText => Time.ToString("G", CultureInfo.CurrentCulture);

        // Fuer die Einfaerbung im Protokoll-Tab (Style-Klassen).
        public bool IsWarning => Level == LogLevel.Warning;
        public bool IsError => Level == LogLevel.Error;

        /// <summary>Lokalisierte Stufe fuer die Anzeige.</summary>
        public string LevelText => LanguageManager.Get(Level switch
        {
            LogLevel.Error => "log_level_error",
            LogLevel.Warning => "log_level_warning",
            _ => "log_level_info",
        });
    }

    /// <summary>
    /// Ereignisprotokoll der GUI selbst (Verbindungswechsel, fehlgeschlagene Kommandos, Updates,
    /// Einstellungen) - NICHT das Log des appleJuice-Cores, den die Schnittstelle nicht liefert.
    /// Nur im Speicher, neueste Eintraege zuerst, hoechstens <see cref="MaxEntries"/>.
    ///
    /// Es duerfen nur selbst formulierte Meldungen hinein, niemals rohe Anfragen oder Links:
    /// jede Core-Anfrage traegt das Passwort (MD5) in der Query.
    /// Aufrufe sind aus jedem Thread erlaubt; die Liste wird auf dem UI-Thread veraendert.
    /// </summary>
    public static class EventLog
    {
        public const int MaxEntries = 500;

        public static ObservableCollection<LogEntry> Entries { get; } = new();

        // key = Schluessel aus den Sprachdateien (Assets/i18n), args = Platzhalter {0}, {1} ...
        // Bewusst nur Schluessel, keine freien Texte: so landet nie versehentlich eine rohe
        // Anfrage (mit Passwort) im Protokoll.
        public static void Info(string key, params object[] args) => Add(LogLevel.Info, key, args);
        public static void Warning(string key, params object[] args) => Add(LogLevel.Warning, key, args);
        public static void Error(string key, params object[] args) => Add(LogLevel.Error, key, args);

        public static void Clear() => Dispatcher.UIThread.Post(() => Entries.Clear());

        /// <summary>Alle Eintraege als Text (aelteste zuerst) fuer die Zwischenablage.</summary>
        public static string ToText()
        {
            var sb = new StringBuilder();
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];
                sb.Append(e.TimeText).Append('\t').Append(e.LevelText).Append('\t').AppendLine(e.Message);
            }
            return sb.ToString();
        }

        // Der Text wird auf dem UI-Thread aufgeloest (Ressourcenzugriff) und bleibt danach in der
        // Sprache, in der er entstand - auch nach einem Sprachwechsel.
        private static string Format(string key, object[] args)
        {
            string text = LanguageManager.Get(key);
            try { return args.Length == 0 ? text : string.Format(text, args); }
            catch (FormatException) { return text; }
        }

        private static void Add(LogLevel level, string key, object[] args)
        {
            DateTime now = DateTime.Now;
            Dispatcher.UIThread.Post(() =>
            {
                Entries.Insert(0, new LogEntry(now, level, Format(key, args)));
                while (Entries.Count > MaxEntries) Entries.RemoveAt(Entries.Count - 1);
            });
        }
    }
}
