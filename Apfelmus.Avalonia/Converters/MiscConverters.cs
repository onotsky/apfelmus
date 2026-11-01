using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Apfelmus.Avalonia.Services;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>Sekunden (Restzeit) -> "hh:mm:ss" bzw. "d.hh:mm:ss".</summary>
    public sealed class TimeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int s && s > 0)
            {
                var t = TimeSpan.FromSeconds(s);
                return t.TotalDays >= 1
                    ? $"{(int)t.TotalDays}.{t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}"
                    : $"{t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}";
            }
            return "-";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Unix-Zeitstempel (Millisekunden) -> lokales Datum/Uhrzeit.</summary>
    public sealed class SecondsToDateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                long ms = value switch
                {
                    long l => l,
                    int i => i,
                    _ => 0
                };
                if (ms <= 0) return "-";
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime.ToString("g", culture);
            }
            catch
            {
                return "-";
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Powerdownload-Wert -> Text (0 = aus, sonst Wert).</summary>
    public sealed class PowerDownloadConverter : IValueConverter
    {
        // Core-Wert -> Anzeige als Verhaeltnis 1:X mit X = (wert+10)/10: 0 -> 1:1 (kein Powerdownload),
        // 12 -> 1:2,2 ... 490 -> 1:50.
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int i = value is int v && v > 0 ? v : 0;
            return "1:" + ((i + 10) / 10.0).ToString("0.#", CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Download-Status -> dezente Zeilen-Hintergrundfarbe (analog WPF DownloadsColor).</summary>
    public sealed class DownloadRowBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int s = value is int i ? i : -999;
            return s switch
            {
                2 => new SolidColorBrush(Color.FromArgb(40, 76, 175, 125)),    // überträgt
                14 => new SolidColorBrush(Color.FromArgb(50, 0, 160, 0)),       // fertig
                18 => new SolidColorBrush(Color.FromArgb(40, 150, 150, 150)),   // pausiert
                13 or 17 => new SolidColorBrush(Color.FromArgb(45, 200, 60, 60)), // Fehler/abgebrochen
                _ => Brushes.Transparent
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Such-Treffer-Markierung: 1 = im eigenen Share vorhanden (gruen, analog WPF SearchColor),
    /// 2 = vorhanden, aber nicht im aktiven Freigabeordner (rot), 3 = wird gerade geladen (gelb), sonst keine.</summary>
    public sealed class SearchRowBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int s = value is int i ? i : 0;
            return s switch
            {
                1 => new SolidColorBrush(Color.FromArgb(55, 0, 157, 0)),     // im Share -> gruen
                2 => new SolidColorBrush(Color.FromArgb(45, 200, 60, 60)),   // vorhanden, nicht aktiv -> rot
                3 => new SolidColorBrush(Color.FromArgb(60, 230, 190, 0)),   // laeuft als Download -> gelb
                _ => Brushes.Transparent
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Server verbunden -> gruener Zeilen-Hintergrund (analog WPF ServerColor).</summary>
    public sealed class ServerRowBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b
                ? new SolidColorBrush(Color.FromArgb(55, 76, 175, 125))
                : Brushes.Transparent;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Status einer Download-Quelle (User) -> lesbarer Text.</summary>
    public sealed class UserStatusConverter : IValueConverter
    {
        // Quellen-Status (andere Codes als der Download-Status!), laut Core-API-PDF:
        // 2 = versuche zu verbinden, 5 = in Warteschlange, 7 = Uebertragung, 14 = Queue voll (Gegenseite).
        // Texte aus den Sprachdateien (Schluessel us_N, unbekannte Codes us_unknown).
        private static readonly HashSet<int> Known = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16 };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int c)
                return LanguageManager.Get(Known.Contains(c) ? "us_" + c : "us_unknown");
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
