using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Bildet den Download-Statuscode des Cores auf einen lesbaren Text ab
    /// (deutsche Texte, analog zu den downloadstatusN-Schluesseln des WPF-Clients).
    /// </summary>
    public sealed class DownloadStatusConverter : IValueConverter
    {
        private static readonly Dictionary<int, string> Map = new()
        {
            [0] = "suchen",
            [1] = "temp. Fehler",
            [2] = "überträgt",
            [12] = "fertigstellen",
            [13] = "Fehler beim Fertigstellen",
            [14] = "fertig",
            [15] = "abbrechen",
            [16] = "in Erstellung",
            [17] = "abgebrochen",
            [18] = "pausiert",
            [19] = "undefiniert",
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int code && Map.TryGetValue(code, out var text))
            {
                return text;
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
