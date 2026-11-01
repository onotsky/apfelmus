using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Apfelmus.Avalonia.Services;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Bildet den Download-Statuscode des Cores auf einen lesbaren Text ab
    /// (Texte aus den Sprachdateien, Schluessel ds_N - analog zu den downloadstatusN des WPF-Clients).
    /// Der Text wird beim Aufbereiten der Zeile gelesen; nach einem Sprachwechsel zeigt eine Zeile
    /// ihn neu, sobald sich ihr Status aendert.
    /// </summary>
    public sealed class DownloadStatusConverter : IValueConverter
    {
        private static readonly HashSet<int> Known = new() { 0, 1, 2, 12, 13, 14, 15, 16, 17, 18, 19 };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int code && Known.Contains(code))
            {
                return LanguageManager.Get("ds_" + code);
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
