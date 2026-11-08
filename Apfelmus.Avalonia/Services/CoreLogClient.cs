using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>Eine Zeile des Core-Logs (vom Log-Dienst ajcore-logd geliefert).</summary>
    public sealed class CoreLogEntry
    {
        public CoreLogEntry(DateTime time, string message) { Time = time; Message = message; }
        public DateTime Time { get; }
        public string Message { get; }
        public string TimeText => Time.ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>Core-Log im Speicher, neueste Zeilen zuerst, hoechstens <see cref="MaxEntries"/>.</summary>
    public static class CoreLog
    {
        public const int MaxEntries = 2000;
        public static ObservableCollection<CoreLogEntry> Entries { get; } = new();

        public static void Clear() => Dispatcher.UIThread.Post(() => Entries.Clear());

        /// <summary>Alle Zeilen als Text (aelteste zuerst) fuer die Zwischenablage.</summary>
        public static string ToText()
        {
            var sb = new StringBuilder();
            for (int i = Entries.Count - 1; i >= 0; i--)
                sb.Append(Entries[i].TimeText).Append('\t').AppendLine(Entries[i].Message);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Holt in festem Abstand neue Zeilen vom Log-Dienst ajcore-logd (GET /log?since=..) und traegt sie
    /// in <see cref="CoreLog"/> ein. Das Token geht nur im Authorization-Header raus und landet nie in
    /// Meldungen oder im Protokoll. Aenderungen von URL/Token wirken ueber <see cref="Configure"/>.
    /// </summary>
    public sealed class CoreLogClient
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

        private CancellationTokenSource? _cts;
        private ulong _since;
        private long _epoch;
        private int _state; // 0 = unbekannt/aus, 1 = ok, 2 = Fehler (nur Wechsel werden protokolliert)

        /// <summary>Normalisiert die Eingabe zu einer Basis-URL ohne Schluss-Slash; null bei leerer/ungueltiger Eingabe.</summary>
        public static string? NormalizeUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string s = input.Trim();
            if (!s.Contains("://")) s = "http://" + s;
            if (!Uri.TryCreate(s, UriKind.Absolute, out var u) || (u.Scheme != "http" && u.Scheme != "https")) return null;
            return s.TrimEnd('/');
        }

        public void Configure(string? url, string? token)
        {
            _cts?.Cancel();
            _cts = null;
            _since = 0;
            _epoch = 0;
            _state = 0;
            string? baseUrl = NormalizeUrl(url);
            if (baseUrl == null || string.IsNullOrWhiteSpace(token)) return;
            var cts = new CancellationTokenSource();
            _cts = cts;
            _ = RunAsync(baseUrl, token.Trim(), cts.Token);
        }

        private async Task RunAsync(string baseUrl, string token, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await PollOnceAsync(baseUrl, token, ct);
                try { await Task.Delay(Interval, ct); } catch (OperationCanceledException) { return; }
            }
        }

        private void SetState(int state, string key)
        {
            if (state == _state) return;
            _state = state;
            if (state == 1) EventLog.Info(key); else EventLog.Warning(key);
        }

        private async Task PollOnceAsync(string baseUrl, string token, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/log?since={_since}&limit=500");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var resp = await Http.SendAsync(req, ct);
                if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    SetState(2, "log_corelog_denied");
                    return;
                }
                if (!resp.IsSuccessStatusCode) { SetState(2, "log_corelog_failed"); return; }

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;
                long epoch = root.GetProperty("epoch").GetInt64();
                if (_epoch != 0 && epoch != _epoch)
                {
                    // Der Log-Dienst wurde neu gestartet: sein Zaehler beginnt von vorn.
                    _epoch = epoch;
                    _since = 0;
                    CoreLog.Clear();
                    return;
                }
                _epoch = epoch;

                var batch = new List<CoreLogEntry>();
                foreach (var e in root.GetProperty("entries").EnumerateArray())
                {
                    DateTime t = DateTimeOffset.TryParse(e.GetProperty("time").GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var dto) ? dto.LocalDateTime : DateTime.Now;
                    batch.Add(new CoreLogEntry(t, e.GetProperty("text").GetString() ?? string.Empty));
                }
                _since = root.GetProperty("next").GetUInt64();
                SetState(1, "log_corelog_up");
                if (batch.Count == 0) return;
                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var b in batch) CoreLog.Entries.Insert(0, b);
                    while (CoreLog.Entries.Count > CoreLog.MaxEntries) CoreLog.Entries.RemoveAt(CoreLog.Entries.Count - 1);
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception) { SetState(2, "log_corelog_failed"); }
        }
    }
}
