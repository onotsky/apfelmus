using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Wandelt eine Byte-Anzahl (long/int/string) in eine menschenlesbare Groesse (KB/MB/GB/TB).
    /// Fasst die vier WPF-FileSizeConverter zu einem generischen Converter zusammen.
    /// </summary>
    public sealed class FileSizeConverter : IValueConverter
    {
        private static readonly string[] Units = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!TryGetBytes(value, out double bytes))
            {
                return value?.ToString() ?? string.Empty;
            }

            int unit = 0;
            while (bytes >= 1024 && unit < Units.Length - 1)
            {
                bytes /= 1024;
                unit++;
            }

            return string.Format(culture, "{0:0.##} {1}", bytes, Units[unit]);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static bool TryGetBytes(object? value, out double bytes)
        {
            switch (value)
            {
                case long l:
                    bytes = l;
                    return true;
                case int i:
                    bytes = i;
                    return true;
                case double d:
                    bytes = d;
                    return true;
                case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed):
                    bytes = parsed;
                    return true;
                default:
                    bytes = 0;
                    return false;
            }
        }
    }
}
