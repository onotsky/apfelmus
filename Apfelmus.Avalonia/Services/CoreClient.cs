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
    /// Bewusst UI-frameworkunabhaengig - lauft auf jeder Plattform.
    /// </summary>
    public sealed class CoreClient
    {
        private readonly Config _config;

        public CoreClient(Config config)
        {
            _config = config;
        }

        public Config Config => _config;

        /// <summary>Prueft, ob der Core erreichbar ist (Socket-Test).</summary>
        public Task<bool> CheckConnectionAsync()
        {
            return Task.Run(() =>
            {
                using var web = new WebConnect(_config.HostName, _config.Port);
                return web.CheckSocket(_config.HostName, _config.Port);
            });
        }

        /// <summary>Holt information.xml (Netzwerk-/Allgemeininfos) und deserialisiert es.</summary>
        public Task<AppleJuice?> GetInformationAsync()
        {
            string query = string.Format(
                "/xml/information.xml?timestamp=0&password={0}&mode=zip", _config.Password);
            return QueryAsync(query);
        }

        /// <summary>Holt einen modified.xml-Teilbereich (z.B. filter=informations, down, uploads, server, search).</summary>
        public Task<AppleJuice?> GetModifiedAsync(string filter)
        {
            string query = string.Format(
                "/xml/modified.xml?timestamp=0&filter={0}&password={1}&mode=zip", filter, _config.Password);
            return QueryAsync(query);
        }

        /// <summary>Holt share.xml (freigegebene Dateien).</summary>
        public Task<AppleJuice?> GetShareAsync()
        {
            string query = string.Format("/xml/share.xml?timestamp=0&password={0}&mode=zip", _config.Password);
            return QueryAsync(query);
        }

        /// <summary>Startet eine Suche (Kommando ohne Antwort). Ergebnisse via GetModifiedAsync("search").</summary>
        public Task StartSearchAsync(string searchText)
        {
            string encoded = Uri.EscapeDataString(searchText ?? string.Empty);
            return FireFunctionAsync("/function/search?search=" + encoded + "&password=" + _config.Password);
        }

        /// <summary>Bricht einen Download ab.</summary>
        public Task CancelDownloadAsync(int id)
            => FireFunctionAsync("/function/canceldownload?id=" + id + "&password=" + _config.Password);

        /// <summary>Pausiert einen Download.</summary>
        public Task PauseDownloadAsync(int id)
            => FireFunctionAsync("/function/pausedownload?id=" + id + "&password=" + _config.Password);

        /// <summary>Setzt einen pausierten Download fort.</summary>
        public Task ResumeDownloadAsync(int id)
            => FireFunctionAsync("/function/resumedownload?id=" + id + "&password=" + _config.Password);

        /// <summary>Verbindet mit einem Server der Liste.</summary>
        public Task ConnectServerAsync(int serverId)
            => FireFunctionAsync("/function/serverlogin?id=" + serverId + "&password=" + _config.Password);

        /// <summary>
        /// Startet einen Download zu einem ajfsp-Datei-Link (Name|Hash|Groesse) ueber processlink -
        /// derselbe Weg, den auch der WPF-Client fuer Links/Suchtreffer nutzt.
        /// </summary>
        public Task StartDownloadAsync(string fileName, string hash, string size)
        {
            string ajfsp = "ajfsp://file%7C" + Uri.EscapeDataString(fileName ?? string.Empty) + "%7C" + hash + "%7C" + size + "/";
            return FireFunctionAsync("/function/processlink?link=" + ajfsp + "&password=" + _config.Password);
        }

        private Task FireFunctionAsync(string function)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var web = new WebConnect(_config.HostName, _config.Port);
                    web.StartXMLFunction(function);
                }
                catch (Exception)
                {
                    // Kommando-Fehler hier schlucken; der Poll-Zyklus spiegelt den Effekt ohnehin wider.
                }
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
                    if (string.IsNullOrWhiteSpace(xml))
                    {
                        return null;
                    }

                    return appleJuice.DeserializeToObj(xml) as AppleJuice;
                }
                catch (Exception)
                {
                    // Netzwerk-/Parsefehler hier schlucken - der aufrufende Poll-Zyklus laeuft weiter.
                    return null;
                }
            });
        }
    }
}
