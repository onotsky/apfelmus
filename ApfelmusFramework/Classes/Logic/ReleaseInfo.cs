using System;
using System.Diagnostics;

namespace ApfelmusFramework.Classes.Logic
{
    /// <summary>
    /// Oeffnet zu einer Datei die "Suche nach mehr Informationen"-Seite im Standardbrowser.
    /// Der ajfsp-Link der Datei wird als GET-Parameter an einen ueber Config.ReleaseInfoHost
    /// konfigurierbaren Host uebergeben. Es werden KEINE Daten inline abgerufen/angezeigt; es wird
    /// lediglich die externe Seite geoeffnet.
    /// </summary>
    public static class ReleaseInfo
    {
        /// <summary>
        /// Standard-Host der Info-Suche. Der Platzhalter %s wird durch den ajfsp-Link der Datei
        /// ersetzt; ueber Config.ReleaseInfoHost ueberschreibbar.
        /// </summary>
        public const string DefaultHost = "https://www.apple-deluxe.co/index.php?ct=403&va=%s";

        /// <summary>
        /// Baut aus Dateiname, Hash und Groesse den ajfsp-Link, setzt ihn in den Host-Platzhalter %s
        /// ein und oeffnet die resultierende URL im Standardbrowser. Ohne Dateiname oder Hash passiert
        /// nichts.
        /// </summary>
        public static void Open(string host, string fileName, string hash, string size)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(hash))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                host = DefaultHost;
            }
            if (string.IsNullOrWhiteSpace(host))
            {
                return; // kein Host konfiguriert -> nichts oeffnen
            }

            // Nur der Dateiname wird kodiert; die Pipes bleiben als %7C stehen (wie im Original-GUI).
            // Hash (Hex) und Groesse (Ziffern) sind bereits URL-sicher.
            string ajfsp = "ajfsp://file%7C" + Uri.EscapeDataString(fileName) + "%7C" + hash + "%7C" + size + "/";
            string url = host.Replace("%s", ajfsp);

            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}
