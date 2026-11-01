using System;
using System.Linq;
using System.Threading.Tasks;
using ApfelmusFramework.Classes.Allgemein;
using Config = ApfelmusFramework.Classes.Config.Config;
using ApfelmusFramework.Classes.Logic;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Duenne, asynchrone Huelle um die plattformneutrale <see cref="WebConnect"/>-Kommunikation
    /// des ApfelmusFramework. Baut die /xml/*-Abfragen und /function/*-Kommandos (identisch zum
    /// WPF-Client) und liefert das deserialisierte <see cref="AppleJuice"/>-Wurzelobjekt.
    /// Bewusst UI-frameworkunabhaengig - laeuft auf jeder Plattform.
    /// </summary>
    public sealed class CoreClient
    {
        private readonly Config _config;

        public CoreClient(Config config)
        {
            _config = config;
        }

        public Config Config => _config;

        public Task<bool> CheckConnectionAsync()
        {
            return Task.Run(() =>
            {
                using var web = new WebConnect(_config.HostName, _config.Port);
                return web.CheckSocket(_config.HostName, _config.Port);
            });
        }

        /// <summary>information.xml: Netzwerk- und Allgemeininfos (Start-Tab, Core-Version).</summary>
        public Task<AppleJuice?> GetInformationAsync()
            => QueryAsync(string.Format("/xml/information.xml?timestamp=0&password={0}&mode=zip", _config.Password));

        /// <summary>modified.xml?filter=informations: Client-Kennzahlen (Credits, Speeds, Session, Verbindungen) + NetworkInfo.</summary>
        public Task<AppleJuice?> GetInformationsAsync()
            => GetModifiedAsync("informations");

        public Task<AppleJuice?> GetModifiedAsync(string filter)
            => QueryAsync(string.Format("/xml/modified.xml?timestamp=0&filter={0}&password={1}&mode=zip", filter, _config.Password));

        public Task<AppleJuice?> GetShareAsync()
            => QueryAsync(string.Format("/xml/share.xml?timestamp=0&password={0}&mode=zip", _config.Password));

        /// <summary>directory.xml: Verzeichnisknoten. path=null liefert die Wurzeln, sonst die Unterordner.</summary>
        public Task<AppleJuice?> GetDirectoryAsync(string? path)
        {
            string q = string.IsNullOrEmpty(path)
                ? "/xml/directory.xml?password=" + _config.Password + "&mode=zip"
                : "/xml/directory.xml?directory=" + Uri.EscapeDataString(path!) + "&password=" + _config.Password + "&mode=zip";
            return QueryAsync(q);
        }

        /// <summary>Setzt die komplette Freigabeliste (setsettings countshares) - wie der WPF-Client.</summary>
        public Task SetSharesAsync(System.Collections.Generic.IList<(string path, bool sub)> shares)
        {
            var sb = new System.Text.StringBuilder("/function/setsettings?countshares=" + shares.Count);
            for (int i = 0; i < shares.Count; i++)
            {
                sb.Append("&sharedirectory").Append(i + 1).Append('=').Append(Uri.EscapeDataString(shares[i].path ?? string.Empty));
                sb.Append("&sharesub").Append(i + 1).Append('=').Append(shares[i].sub ? "true" : "false");
            }
            sb.Append("&password=").Append(_config.Password);
            return Fire(sb.ToString());
        }

        /// <summary>downloadpartlist.xml: Verfuegbarkeitssegmente (Parts) + Dateigroesse eines Downloads.</summary>
        public Task<AppleJuice?> GetDownloadPartlistAsync(int downloadId)
            => QueryAsync(string.Format("/xml/downloadpartlist.xml?id={0}&password={1}&mode=zip", downloadId, _config.Password));

        /// <summary>Liest die Core-Einstellungen (settings.xml).</summary>
        public Task<ApfelmusFramework.Classes.Settings.Settings?> GetSettingsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using var web = new WebConnect(_config.HostName, _config.Port);
                    var settings = new ApfelmusFramework.Classes.Settings.Settings();
                    string xml = web.GetHttpResult("/xml/settings.xml?password=" + _config.Password + "&mode=zip", _config.UseCompression);
                    if (string.IsNullOrWhiteSpace(xml)) return null;
                    return settings.DeserializeToObj(xml) as ApfelmusFramework.Classes.Settings.Settings;
                }
                catch (Exception) { return null; }
            });
        }

        /// <summary>Schreibt die Core-Einstellungen (setsettings). Verzeichnisse werden URL-kodiert.</summary>
        public Task SetSettingsAsync(ApfelmusFramework.Classes.Settings.Settings s)
        {
            string q = "/function/setsettings?"
                + "Incomingdirectory=" + Uri.EscapeDataString(s.IncomingDirectory ?? string.Empty) + "&"
                + "Temporarydirectory=" + Uri.EscapeDataString(s.TemporaryDirectory ?? string.Empty) + "&"
                + "Port=" + s.Port + "&"
                + "XMLPort=" + s.XmlPort + "&"
                + "Nickname=" + Uri.EscapeDataString(s.Nick ?? string.Empty) + "&"
                + "MaxConnections=" + s.MaxConnections + "&"
                + "MaxDownload=" + s.MaxDownload + "&"
                + "MaxUpload=" + s.MaxUpload + "&"
                + "Speedperslot=" + s.SpeedPerSlot + "&"
                + "MaxNewConnectionsPerTurn=" + s.MaxNewConnectionsPerTurn + "&"
                + "MaxSourcesPerFile=" + s.MaxSourcesPerFile + "&"
                + "password=" + _config.Password;
            return Fire(q);
        }

        /// <summary>Aendert das Core-Passwort (Klartext -> MD5) und liefert den neuen MD5-Wert zurueck.</summary>
        public Task<string> SetPasswordAsync(string newPlainPassword)
        {
            return Task.Run(() =>
            {
                string newMd5 = CreateMd5Hash.GetMD5Hash(newPlainPassword);
                try
                {
                    using var web = new WebConnect(_config.HostName, _config.Port);
                    web.StartXMLFunction("/function/setpassword?newpassword=" + newMd5 + "&password=" + _config.Password);
                }
                catch (Exception) { }
                return newMd5;
            });
        }

        // ---- Kommandos (/function/*) -----------------------------------------------------------

        public Task StartSearchAsync(string searchText)
            => Fire("/function/search?search=" + Uri.EscapeDataString(searchText ?? string.Empty) + "&password=" + _config.Password);

        public Task StopSearchAsync(int searchId)
            => Fire("/function/cancelsearch?id=" + searchId + "&password=" + _config.Password);

        public Task CancelDownloadAsync(int id)
            => Fire("/function/canceldownload?id=" + id + "&password=" + _config.Password);

        public Task PauseDownloadAsync(int id)
            => Fire("/function/pausedownload?id=" + id + "&password=" + _config.Password);

        public Task ResumeDownloadAsync(int id)
            => Fire("/function/resumedownload?id=" + id + "&password=" + _config.Password);

        public Task CleanDownloadListAsync()
            => Fire("/function/cleandownloadlist?password=" + _config.Password);

        public Task RenameDownloadAsync(int id, string newName)
            => Fire("/function/renamedownload?id=" + id + "&name=" + Uri.EscapeDataString(newName ?? string.Empty) +"&password=" + _config.Password);

        /// <summary>Setzt das Zielverzeichnis eines Downloads (Pfad auf dem Core-Rechner).</summary>
        public Task SetTargetDirAsync(int id, string dir)
            => Fire("/function/settargetdir?id=" + id + "&dir=" + Uri.EscapeDataString(dir ?? string.Empty) + "&password=" + _config.Password);

        public Task SetDownloadPriorityAsync(int id, int priority)
            => Fire("/function/setpriority?id=" + id + "&priority=" + priority + "&password=" + _config.Password);

        public Task SetPowerDownloadAsync(int id, int value)
            => Fire("/function/setpowerdownload?id=" + id + "&Powerdownload=" + value + "&password=" + _config.Password);

        public Task ConnectServerAsync(int serverId)
            => Fire("/function/serverlogin?id=" + serverId + "&password=" + _config.Password);

        public Task RemoveServerAsync(int serverId)
            => Fire("/function/removeserver?id=" + serverId + "&password=" + _config.Password);

        public Task ExitCoreAsync()
            => Fire("/function/exitcore?password=" + _config.Password);

        /// <summary>Startet einen Download/verarbeitet einen ajfsp-Link (AJLink-Transfer, Suchtreffer).</summary>
        public Task ProcessLinkAsync(string ajfspLink)
            => Fire("/function/processlink?link=" + ajfspLink + "&password=" + _config.Password);

        /// <summary>Baut aus Name/Hash/Groesse einen ajfsp-Datei-Link und startet den Download.</summary>
        public Task StartDownloadAsync(string fileName, string hash, string size)
        {
            string ajfsp = "ajfsp://file%7C" + Uri.EscapeDataString(fileName ?? string.Empty) + "%7C" + hash + "%7C" + size + "/";
            return ProcessLinkAsync(ajfsp);
        }

        /// <summary>Holt die offizielle Serverliste und uebergibt jeden Server-Link an den Core.</summary>
        public Task AddOfficialServersAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var list = WebConnect.GetXMLServerList();
                    if (list?.Server == null) return;
                    foreach (var s in list.Server)
                    {
                        // Die Liste kommt von extern (unverschluesselt) -> nur Server-Links durchlassen
                        // und wie alle anderen Links einheitlich kodieren (kein Parameter-Einschleusen).
                        if (s?.Link == null || !s.Link.StartsWith("ajfsp://server|", StringComparison.OrdinalIgnoreCase)) continue;
                        using var web = new WebConnect(_config.HostName, _config.Port);
                        web.StartXMLFunction("/function/processlink?link=" + Uri.EscapeDataString(Uri.UnescapeDataString(s.Link)) + "&password=" + _config.Password);
                    }
                }
                catch (Exception) { }
            });
        }

        private Task Fire(string function)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var web = new WebConnect(_config.HostName, _config.Port);
                    var reply = web.ExecuteCommand(function);
                    var action = ActionKey(function);
                    if (!reply.Sent)
                        EventLog.Warning("log_cmd_failed", action);
                    else if (reply.StatusCode is 401 or 403)
                        EventLog.Warning("log_cmd_denied", action);
                    else if (reply.StatusCode >= 400)
                        EventLog.Warning("log_cmd_rejected", action, reply.StatusCode);
                    else
                        LogReply(function, action, reply.Body);
                }
                catch (Exception) { EventLog.Warning("log_cmd_failed", ActionKey(function)); }
            });
        }

        // Die Antworttexte des Cores sind nicht dokumentiert. Normalfall sind eine leere Antwort, "ok"
        // und - wie beim Core des Maintainers fuer /function/search beobachtet - ein leeres
        // HTML-Geruest (<html><body></body></html>). Bekannt ist "already downloaded"; alles andere
        // wird als Info mit dem Text des Cores ins Protokoll geschrieben, nicht als Fehler gewertet.
        private static void LogReply(string function, LogKey action, string body)
        {
            string text = StripTags(body);
            if (text.Length == 0 || text.TrimEnd('.', '!').Equals("ok", StringComparison.OrdinalIgnoreCase))
                return;

            if (text.Contains("already downloaded", StringComparison.OrdinalIgnoreCase))
            {
                string? name = LinkInfo.FileName(QueryValue(function, "link"));
                if (name != null) EventLog.Info("log_already_file", name);
                else EventLog.Info("log_already");
                return;
            }

            EventLog.Info("log_cmd_reply", action, ShortReply(text));
        }

        private static string StripTags(string body)
            => System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", " ").Trim();

        // Antwort fuer die Anzeige: Steuerzeichen raus, auf 120 Zeichen gekuerzt.
        private static string ShortReply(string text)
        {
            var chars = text.Select(c => char.IsControl(c) ? ' ' : c).ToArray();
            string clean = new string(chars).Trim();
            return clean.Length <= 120 ? clean : clean.Substring(0, 120) + "…";
        }

        // Bezeichnung des Befehls fuer Meldungen (z.B. "Download abbrechen") - nur aus dem Pfad
        // abgeleitet, nie aus der Query: die traegt das Passwort und darf nicht ins Protokoll.
        private static readonly System.Collections.Generic.HashSet<string> KnownCommands = new()
        {
            "canceldownload", "cancelsearch", "cleandownloadlist", "exitcore", "pausedownload",
            "processlink", "removeserver", "renamedownload", "resumedownload", "search", "serverlogin",
            "setpassword", "setpowerdownload", "setpriority", "setsettings", "settargetdir",
        };

        private static LogKey ActionKey(string function)
        {
            int q = function.IndexOf('?');
            string path = q >= 0 ? function.Substring(0, q) : function;
            string name = path.Substring(path.LastIndexOf('/') + 1);
            return new LogKey(KnownCommands.Contains(name) ? "cmd_" + name : "cmd_unknown");
        }

        // Liest EINEN Parameter aus der Query (dekodiert); die uebrigen - v.a. password - bleiben unberuehrt.
        private static string? QueryValue(string function, string key)
        {
            int q = function.IndexOf('?');
            if (q < 0) return null;
            foreach (string part in function.Substring(q + 1).Split('&'))
            {
                if (part.StartsWith(key + "=", StringComparison.Ordinal))
                    return Uri.UnescapeDataString(part.Substring(key.Length + 1));
            }
            return null;
        }

        private Task<AppleJuice?> QueryAsync(string query)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var web = new WebConnect(_config.HostName, _config.Port);
                    var appleJuice = new AppleJuice();
                    string xml = web.GetHttpResult(query, _config.UseCompression);
                    if (string.IsNullOrWhiteSpace(xml)) return null;
                    return appleJuice.DeserializeToObj(xml) as AppleJuice;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }
    }
}
