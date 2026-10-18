using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>Ergebnis der Core-Versionspruefung.</summary>
    public sealed class CoreUpdateResult
    {
        public string LatestVersion { get; init; } = string.Empty; // z.B. "0.35.186.1"
        public string HtmlUrl { get; init; } = string.Empty;       // Release-Seite auf GitHub
        public bool IsPrerelease { get; init; }                     // Beta (GitHub "prerelease")
        public bool IsNewer { get; init; }                          // neuer als der laufende Core?
    }

    /// <summary>
    /// Prueft gegen die GitHub-Releases von applejuicenetz/core, ob es einen neueren Core gibt -
    /// stabile Versionen und Betas (Prereleases). Gemeldet wird die hoechste Versionsnummer; ob
    /// sie eine Beta ist, steht in <see cref="CoreUpdateResult.IsPrerelease"/>. Tags ohne reine
    /// Versionsnummer (z.B. "yguard-mappings") werden ignoriert. Bei Netzwerk-/Parsefehlern null.
    /// </summary>
    public static class CoreUpdateChecker
    {
        private const string ReleasesApi =
            "https://api.github.com/repos/applejuicenetz/core/releases?per_page=30";

        private static readonly Regex VersionTag = new(@"^\d+(\.\d+)+$");

        public static async Task<CoreUpdateResult?> CheckAsync(string currentCoreVersion)
        {
            if (!VersionTag.IsMatch(currentCoreVersion ?? string.Empty)) return null;
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Apfelmus-CoreUpdateCheck");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                using var doc = JsonDocument.Parse(await http.GetStringAsync(ReleasesApi));
                CoreUpdateResult? best = null;
                foreach (var rel in doc.RootElement.EnumerateArray())
                {
                    if (rel.TryGetProperty("draft", out var dr) && dr.GetBoolean()) continue;
                    string tag = rel.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                    if (!VersionTag.IsMatch(tag)) continue;
                    if (best != null && UpdateChecker.Compare(tag, best.LatestVersion) <= 0) continue;
                    best = new CoreUpdateResult
                    {
                        LatestVersion = tag,
                        HtmlUrl = rel.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "",
                        IsPrerelease = rel.TryGetProperty("prerelease", out var p) && p.GetBoolean(),
                        IsNewer = UpdateChecker.Compare(tag, currentCoreVersion!) > 0,
                    };
                }
                return best;
            }
            catch
            {
                return null;
            }
        }
    }
}
