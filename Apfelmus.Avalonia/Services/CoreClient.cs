using System;
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
                        using var web = new WebConnect(_config.HostName, _config.Port);
                        web.StartXMLFunction("/function/processlink?link=" + s.Link + "&password=" + _config.Password);
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
                    web.StartXMLFunction(function);
                }
                catch (Exception) { }
            });
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
