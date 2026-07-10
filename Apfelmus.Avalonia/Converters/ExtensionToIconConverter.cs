using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Apfelmus.Avalonia.Converters
{
    /// <summary>
    /// Liefert zu einem Dateinamen ein handgezeichnetes Vektor-Icon (Datei-Seite), dessen Farbe die
    /// Dateikategorie (Archiv/Audio/Video/Bild/Dokument/Programm/sonstiges) codiert. Plattformneutral
    /// (kein Windows-Shell-Icon) und theme-freundlich - passt zum Vektor-Icon-Stil des Clients.
    /// </summary>
    public sealed class ExtensionToIconConverter : IValueConverter
    {
        private static readonly Geometry Page = Geometry.Parse("M5,2 H14 L19,7 V22 H5 Z");
        private static readonly Geometry Fold = Geometry.Parse("M14,2 V7 H19");
        private static readonly Dictionary<string, DrawingImage> Cache = new();

        private static readonly Dictionary<string, Color> Categories = new()
        {
            ["archive"] = Color.FromRgb(0xE0, 0xA0, 0x30),
            ["audio"] = Color.FromRgb(0x4C, 0xAF, 0x7D),
            ["video"] = Color.FromRgb(0xC0, 0x50, 0x4D),
            ["image"] = Color.FromRgb(0x80, 0x64, 0xA2),
            ["document"] = Color.FromRgb(0x4C, 0x6B, 0xFF),
            ["executable"] = Color.FromRgb(0x90, 0x90, 0x90),
            ["generic"] = Color.FromRgb(0x8F, 0xA0, 0xAB),
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string name = value?.ToString() ?? string.Empty;
            string cat = Categorize(name);
            if (Cache.TryGetValue(cat, out var cached)) return cached;

            var color = Categories.TryGetValue(cat, out var c) ? c : Categories["generic"];
            var darker = Color.FromRgb((byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7));

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing
            {
                Geometry = Page,
                Brush = new SolidColorBrush(color),
                Pen = new Pen(new SolidColorBrush(darker), 1)
            });
            group.Children.Add(new GeometryDrawing
            {
                Geometry = Fold,
                Pen = new Pen(new SolidColorBrush(darker), 1)
            });

            var image = new DrawingImage { Drawing = group };
            Cache[cat] = image;
            return image;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static string Categorize(string fileName)
        {
            string ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            return ext switch
            {
                "zip" or "rar" or "7z" or "tar" or "gz" or "bz2" or "xz" => "archive",
                "mp3" or "flac" or "wav" or "ogg" or "m4a" or "aac" or "wma" => "audio",
                "avi" or "mkv" or "mp4" or "mov" or "wmv" or "flv" or "mpg" or "mpeg" or "m4v" => "video",
                "jpg" or "jpeg" or "png" or "gif" or "bmp" or "tif" or "tiff" or "webp" => "image",
                "pdf" or "doc" or "docx" or "txt" or "rtf" or "odt" or "xls" or "xlsx" or "ppt" or "pptx" or "epub" => "document",
                "exe" or "msi" or "dmg" or "app" or "deb" or "rpm" or "iso" or "bin" => "executable",
                _ => "generic",
            };
        }
    }
}
