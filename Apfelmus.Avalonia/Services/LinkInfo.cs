using System;
using System.Linq;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Liest aus einem ajfsp-Link das, was man in einer Meldung zeigen darf: den Dateinamen
    /// (ajfsp://file|Name|Hash|Groesse/) bzw. den Server (ajfsp://server|Host|Port/). Der Link darf
    /// roh oder %-kodiert sein. Liefert null, wenn der Link nicht dieser Form entspricht.
    /// </summary>
    public static class LinkInfo
    {
        public static string? FileName(string? link)
        {
            var parts = Fields(link, "ajfsp://file|");
            return parts is { Length: > 0 } && parts[0].Length > 0 ? Clean(parts[0]) : null;
        }

        public static string? Server(string? link)
        {
            var parts = Fields(link, "ajfsp://server|");
            return parts is { Length: > 1 } && parts[0].Length > 0 ? Clean(parts[0] + ":" + parts[1]) : null;
        }

        private static string[]? Fields(string? link, string prefix)
        {
            if (string.IsNullOrWhiteSpace(link)) return null;
            string l = link.Trim();
            if (l.Contains('%')) l = Uri.UnescapeDataString(l);
            if (!l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
            return l.Substring(prefix.Length).TrimEnd('/').Split('|');
        }

        // Steuerzeichen raus, auf 120 Zeichen gekuerzt (der Name stammt aus fremden Daten).
        private static string Clean(string text)
        {
            string t = new string(text.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
            return t.Length <= 120 ? t : t.Substring(0, 120) + "…";
        }
    }
}
