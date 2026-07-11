using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Farbe des Verbindungs-Indikators in der Statusleiste: gruen = verbunden, rot = nicht verbunden.
    /// </summary>
    public sealed class ConnectedDotConverter : IValueConverter
    {
        private static readonly IBrush Connected = new SolidColorBrush(Color.Parse("#4CAF7D"));
        private static readonly IBrush Disconnected = new SolidColorBrush(Color.Parse("#E06A5B"));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? Connected : Disconnected;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
