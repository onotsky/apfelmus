using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Holt die News-Seite des appleJuice-Projekts (github.com/applejuicenetz/news). Je Core-Version
    /// gibt es eine eigene HTML-Datei; fehlt sie, liefert GitHub Pages die 404.html mit dem
    /// Standardhinweis - deren Body wird bewusst auch bei Status 404 verwendet. Bei Netzwerkfehlern null.
    /// </summary>
    public static class NewsFetcher
    {
        private const string BaseUrl = "https://applejuicenetz.github.io/news/";

        public static async Task<string?> FetchAsync(string coreVersion)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8), MaxResponseContentBufferSize = 2 * 1024 * 1024 };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Apfelmus-News");
                string name = Uri.EscapeDataString(string.IsNullOrWhiteSpace(coreVersion) ? "404" : coreVersion);
                using var resp = await http.GetAsync(BaseUrl + name + ".html");
                string body = await resp.Content.ReadAsStringAsync();
                return body.Contains("<body", StringComparison.OrdinalIgnoreCase) ? body : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
