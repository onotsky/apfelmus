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
    /// <summary>Ergebnis des Partlisten-Renderings: Bild + Zell-Typraster (fuer den Hover-Tooltip).</summary>
    public sealed record PartlistResult(WriteableBitmap Bitmap, int[] Cells, int Columns, int Rows);

    public static class PartlistRenderer
    {
        private const int MaxGradientSources = 10;

        // Sondertypen im Zellraster (Cells) zusaetzlich zu part.type (>=1 = Quellenzahl, -1 = fertig, <=0 = fehlt):
        public const int CellLoaded = -2;   // orange (aktive Quelle, bereits geladener Bereich)
        public const int CellActive = -3;   // gelb (aktuelle Position der Quelle)

        public static PartlistResult? Render(long fileSize, List<Part>? parts, IEnumerable<User>? activeSources,
            int columnsPerRow, int rows)
        {
            if (columnsPerRow <= 0 || rows <= 0 || fileSize <= 0 || parts == null || parts.Count == 0)
                return null;

            int totalColumns = rows * columnsPerRow;
            int[] strip = new int[totalColumns];
            int[] cells = new int[totalColumns]; // Typ je Zelle (fuer Tooltip)

            int ColumnForByte(long bytePosition)
            {
                long column = bytePosition * totalColumns / fileSize;
                if (column < 0) return 0;
                if (column > totalColumns) return totalColumns;
                return (int)column;
            }

            // Pro Zelle werden die Bytes nach Typ aufsummiert und die Zelle als Mischfarbe gezeichnet:
            // Anteil fertig -> gruen, Rest in der Farbe des ueberwiegenden fehlenden Typs. Bei grossen
            // Dateien ist eine Zelle viele MB breit, waehrend der Core in ~0,5-1-MB-Stuecken verstreut
            // laedt - fast jede Zelle ist gemischt. "Erster/letzter gewinnt" zeigte dann entweder kaum
            // Gruen (fehlend gewinnt) oder verschluckte kleine Reste (fertig gewinnt).
            long[] doneBytes = new long[totalColumns];
            var missingBytes = new Dictionary<int, long>[totalColumns];
            double bytesPerColumn = fileSize / (double)totalColumns;

            for (int i = 0; i < parts.Count; i++)
            {
                long from = Math.Max(0, parts[i].FromPosition);
                long to = Math.Min(fileSize, (i + 1 < parts.Count) ? parts[i + 1].FromPosition : fileSize);
                if (to <= from) continue;
                int type = parts[i].type;
                int c = Math.Min(totalColumns - 1, ColumnForByte(from));
                while (c < totalColumns && from < to)
                {
                    long cellEnd = c == totalColumns - 1 ? to : Math.Min(to, (long)Math.Ceiling((c + 1) * bytesPerColumn));
                    long n = cellEnd - from;
                    if (n > 0)
                    {
                        if (type == -1) doneBytes[c] += n;
                        else
                        {
                            var d = missingBytes[c] ??= new Dictionary<int, long>();
                            d[type] = d.TryGetValue(type, out long v) ? v + n : n;
                        }
                        from = cellEnd;
                    }
                    c++;
                }
            }

            int green = ColorForType(-1);
            for (int c = 0; c < totalColumns; c++)
            {
                var missing = missingBytes[c];
                if (missing == null)
                {
                    // Zelle ohne fehlende Bytes: fertig (oder nicht abgedeckt -> wie fehlend behandeln).
                    strip[c] = doneBytes[c] > 0 ? green : ColorForType(0);
                    cells[c] = doneBytes[c] > 0 ? -1 : 0;
                    continue;
                }
                long missingTotal = 0; int mainType = 0; long mainBytes = -1;
                foreach (var kv in missing)
                {
                    missingTotal += kv.Value;
                    if (kv.Value > mainBytes) { mainBytes = kv.Value; mainType = kv.Key; }
                }
                double doneFraction = doneBytes[c] / (double)(doneBytes[c] + missingTotal);
                // Unvollstaendige Zellen hoechstens zu 75 % gruen mischen, damit auch ein kleiner
                // Rest (z.B. 280 KB in einer 3,5-MB-Zelle) sichtbar von "komplett fertig" abweicht.
                strip[c] = Mix(ColorForType(mainType), green, Math.Min(doneFraction, 0.75));
                cells[c] = mainType;
            }

            if (activeSources != null)
            {
                int orange = Argb(255, 255, 165, 0);
                int yellow = Argb(255, 255, 255, 0);
                foreach (var u in activeSources)
                {
                    int fromColumn = ColumnForByte(u.DownloadFrom);
                    int posColumn = ColumnForByte(u.ActualDownloadPosition);
                    for (int c = fromColumn; c < posColumn; c++) { strip[c] = orange; cells[c] = CellLoaded; }
                    if (posColumn < totalColumns) { strip[posColumn] = yellow; cells[posColumn] = CellActive; }
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
            return new PartlistResult(bitmap, cells, columnsPerRow, rows);
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

        /// <summary>Lineare Mischung zweier ARGB-Farben (t = 0 -> a, t = 1 -> b).</summary>
        private static int Mix(int a, int b, double t)
        {
            byte Ch(int shift) => (byte)Math.Round(((a >> shift) & 0xFF) * (1 - t) + ((b >> shift) & 0xFF) * t);
            return Argb(255, Ch(16), Ch(8), Ch(0));
        }

        // Bgra8888: int little-endian ergibt Bytefolge B,G,R,A.
        private static int Argb(byte a, byte r, byte g, byte b) => (a << 24) | (r << 16) | (g << 8) | b;
    }
}
