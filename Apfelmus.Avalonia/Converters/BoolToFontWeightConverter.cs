using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>true = halbfett (z.B. freigegebener Ordner im Baum), false = normal.</summary>
    public sealed class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? FontWeight.SemiBold : FontWeight.Normal;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
