using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

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
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int i && i > 0 ? i.ToString() : "aus";

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
        private static readonly Dictionary<int, string> Map = new()
        {
            [0] = "neu",
            [1] = "verbinde",
            [2] = "verbinde",
            [3] = "Warteschlange",
            [4] = "nicht erreichbar",
            [5] = "Warteschlange",
            [6] = "verbunden",
            [7] = "überträgt",
            [8] = "fertig",
            [11] = "Fehler",
            [12] = "fertig",
            [13] = "Fehler",
            [14] = "Queue voll",
            [15] = "abbrechen",
            [16] = "pausiert",
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int c ? (Map.TryGetValue(c, out var t) ? t : "unbekannt") : (value?.ToString() ?? string.Empty);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
