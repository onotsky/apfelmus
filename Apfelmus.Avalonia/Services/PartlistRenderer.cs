using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ApfelmusFramework.Classes.Allgemein;
using ApfelmusFramework.Classes.Modified;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Zeichnet den Verfuegbarkeitsbalken eines Downloads als <see cref="WriteableBitmap"/> -
    /// Avalonia-Portierung der unabhaengig entworfenen WPF-Logik (RenderPartList): die Datei laeuft
    /// als durchgehender Streifen ueber mehrere Zeilen; jeder Part faerbt [FromPosition, naechste
    /// FromPosition) nach Verfuegbarkeit/Quellenzahl. Aktive Uebertragungen (Quellen) werden
    /// orange (geladen) und gelb (Position) ueberlagert. Das Bild wird von der Image-Control gestreckt.
    /// </summary>
    public static class PartlistRenderer
    {
        private const int MaxGradientSources = 10;

        public static WriteableBitmap? Render(long fileSize, List<Part>? parts, IEnumerable<User>? activeSources,
            int columnsPerRow, int rows)
        {
            if (columnsPerRow <= 0 || rows <= 0 || fileSize <= 0 || parts == null || parts.Count == 0)
                return null;

            int totalColumns = rows * columnsPerRow;
            int[] strip = new int[totalColumns];

            int ColumnForByte(long bytePosition)
            {
                long column = bytePosition * totalColumns / fileSize;
                if (column < 0) return 0;
                if (column > totalColumns) return totalColumns;
                return (int)column;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                long from = parts[i].FromPosition;
                long to = (i + 1 < parts.Count) ? parts[i + 1].FromPosition : fileSize;
                int fromColumn = ColumnForByte(from);
                int toColumn = ColumnForByte(to);
                int color = ColorForType(parts[i].type);
                for (int c = fromColumn; c < toColumn; c++) strip[c] = color;
            }

            if (activeSources != null)
            {
                int orange = Argb(255, 255, 165, 0);
                int yellow = Argb(255, 255, 255, 0);
                foreach (var u in activeSources)
                {
                    int fromColumn = ColumnForByte(u.DownloadFrom);
                    int posColumn = ColumnForByte(u.ActualDownloadPosition);
                    for (int c = fromColumn; c < posColumn; c++) strip[c] = orange;
                    if (posColumn < totalColumns) strip[posColumn] = yellow;
                }
            }

            var bitmap = new WriteableBitmap(new PixelSize(columnsPerRow, rows), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using (var fb = bitmap.Lock())
            {
                // strip ist row-major (columnsPerRow pro Zeile). Zeilenweise kopieren, da RowBytes
                // groesser als columnsPerRow*4 sein kann (Stride-Padding).
                for (int row = 0; row < rows; row++)
                {
                    IntPtr dest = fb.Address + row * fb.RowBytes;
                    Marshal.Copy(strip, row * columnsPerRow, dest, columnsPerRow);
                }
            }
            return bitmap;
        }

        private static int ColorForType(int type)
        {
            if (type == -1) return Argb(255, 0, 128, 0);       // fertig -> gruen
            if (type <= 0) return Argb(255, 255, 0, 0);        // nicht verfuegbar -> rot
            int clamped = Math.Max(1, Math.Min(type, MaxGradientSources));
            double t = (clamped - 1) / (double)(MaxGradientSources - 1);
            byte ch = (byte)Math.Round(220 - (t * 190));
            return Argb(255, ch, ch, 255);                     // je mehr Quellen, desto dunkler blau
        }

        // Bgra8888: int little-endian ergibt Bytefolge B,G,R,A.
        private static int Argb(byte a, byte r, byte g, byte b) => (a << 24) | (r << 16) | (g << 8) | b;
    }
}
