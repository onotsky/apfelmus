using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Formatiert die netzwerkweite Gesamtgroesse. Der Core liefert diesen Wert bereits in
    /// MEGABYTE (nicht in Bytes) und mit Komma als Dezimaltrenner (z.B. "1342846209,61").
    /// Deshalb beginnt die Einheitenskala hier bei MB (analog WPF-FileSizeConverter2) - sonst
    /// wuerde z.B. PB faelschlich als GB angezeigt.
    /// </summary>
    public sealed class NetworkSizeConverter : IValueConverter
    {
        private static readonly string[] Units = { "MB", "GB", "TB", "PB", "EB", "ZB" };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            string s = value.ToString()!.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                return value.ToString();

            int i = 0;
            while (i < Units.Length - 1 && Math.Abs(v) >= 1024)
            {
                v /= 1024;
                i++;
            }
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", v, Units[i]);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
