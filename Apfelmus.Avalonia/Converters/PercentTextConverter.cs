using System;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Wandelt den Fortschrittstext eines Downloads (z.B. "37,25 %" oder "100 %") in einen
    /// numerischen Wert 0..100 fuer die Bindung an <c>ProgressBar.Value</c>. Toleriert Komma-
    /// und Punkt-Dezimaltrennung sowie das angehaengte Prozentzeichen.
    /// </summary>
    public sealed class PercentTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && s.Length > 0)
            {
                var sb = new StringBuilder(s.Length);
                foreach (char c in s)
                {
                    if (char.IsDigit(c)) sb.Append(c);
                    else if (c == ',' || c == '.') sb.Append('.');
                }

                if (sb.Length > 0 &&
                    double.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    return Math.Clamp(d, 0.0, 100.0);
                }
            }

            return 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
