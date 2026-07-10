using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Avalonia-Portierung des gleichnamigen WPF-Converters: waehlt anhand des Firewall-Status
    /// (true = hinter Firewall) das passende Symbol. Statt eines pack-URI-Strings (WPF) wird hier
    /// direkt eine <see cref="Bitmap"/> aus den avares-Assets geliefert.
    /// </summary>
    public sealed class FirewallConverter : IValueConverter
    {
        private static readonly Dictionary<string, Bitmap> Cache = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool firewalled = value is bool b && b;
            string asset = firewalled ? "security_firewall_off.png" : "security_firewall_on.png";
            return Load(asset);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static Bitmap Load(string fileName)
        {
            if (Cache.TryGetValue(fileName, out var cached))
            {
                return cached;
            }

            var uri = new Uri($"avares://Apfelmus.Avalonia/Assets/Images/{fileName}");
            using var stream = AssetLoader.Open(uri);
            var bitmap = new Bitmap(stream);
            Cache[fileName] = bitmap;
            return bitmap;
        }
    }
}
