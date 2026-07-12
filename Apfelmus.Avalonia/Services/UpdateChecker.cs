using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>Ergebnis einer Update-Pruefung gegen die GitHub-Releases-API.</summary>
    public sealed class UpdateResult
    {
        public string LatestVersion { get; init; } = string.Empty; // z.B. "5.3.21"
        public string HtmlUrl { get; init; } = string.Empty;       // Release-Seite
        public bool IsNewer { get; init; }                          // neuer als die laufende Version?
    }

    /// <summary>
    /// Prueft ueber die oeffentliche GitHub-Releases-API, ob eine neuere Version vorliegt.
    /// Kein Git und kein Token noetig - es wird nur das "latest release" (tag_name) gelesen und
    /// mit der laufenden Version verglichen. Bei Netzwerk-/Parsefehlern wird null geliefert
    /// (die App laeuft ohne Update-Info unveraendert weiter).
    /// </summary>
    public static class UpdateChecker
    {
        private const string LatestReleaseApi =
            "https://api.github.com/repos/onotsky/apfelmus/releases/latest";

        public static async Task<UpdateResult?> CheckAsync(string currentVersion)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                // GitHub verlangt einen User-Agent, sonst 403.
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Apfelmus-UpdateCheck");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                string json = await http.GetStringAsync(LatestReleaseApi);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                string url = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";
                string latest = NormalizeVersion(tag);
                if (latest.Length == 0) return null;

                return new UpdateResult
                {
                    LatestVersion = latest,
                    HtmlUrl = url,
                    IsNewer = Compare(latest, currentVersion) > 0,
                };
            }
            catch
            {
                return null;
            }
        }

        // Tags kommen als "v5.3.20" (aktuell) oder frueher "avalonia-v5.3.15" - alles vor der
        // ersten Ziffer wird abgeschnitten, uebrig bleibt die reine Versionsnummer.
        private static string NormalizeVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return string.Empty;
            int i = 0;
            while (i < tag.Length && !char.IsDigit(tag[i])) i++;
            return tag.Substring(i).Trim();
        }

        // Vergleicht zwei Versionsstrings ("5.3.20") numerisch; fehlende Stellen zaehlen als 0.
        private static int Compare(string a, string b)
        {
            var pa = a.Split('.');
            var pb = b.Split('.');
            int n = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < n; i++)
            {
                int va = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
                int vb = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
                if (va != vb) return va.CompareTo(vb);
            }
            return 0;
        }
    }
}
