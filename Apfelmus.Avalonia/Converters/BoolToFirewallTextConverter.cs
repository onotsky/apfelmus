using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Apfelmus.Avalonia.Services;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Liefert fuer den Firewall-Status einen lokalisierten Klartext ("hinter Firewall" / "erreichbar").
    /// </summary>
    public sealed class BoolToFirewallTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool firewalled = value is bool b && b;
            return LanguageManager.Get(firewalled ? "fw_behind" : "fw_reachable");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
