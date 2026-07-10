using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Liefert fuer den Firewall-Status einen Klartext ("hinter Firewall" / "erreichbar").
    /// </summary>
    public sealed class BoolToFirewallTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool firewalled = value is bool b && b;
            return firewalled ? "hinter Firewall" : "erreichbar";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
