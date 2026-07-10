using System;
using System.Threading.Tasks;
using ApfelmusFramework.Classes.Allgemein;
using Config = ApfelmusFramework.Classes.Config.Config;
using ApfelmusFramework.Classes.Logic;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Duenne, asynchrone Huelle um die plattformneutrale <see cref="WebConnect"/>-Kommunikation
    /// des ApfelmusFramework. Baut die /xml/*-Abfragen (identisch zum WPF-Client) und liefert das
    /// deserialisierte <see cref="AppleJuice"/>-Wurzelobjekt. Bewusst UI-frameworkunabhaengig.
    /// </summary>
    public sealed class CoreClient
    {
        private readonly Config _config;

        public CoreClient(Config config)
        {
            _config = config;
        }

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

        /// <summary>Holt einen modified.xml-Teilbereich (z.B. filter=informations, down, uploads).</summary>
        public Task<AppleJuice?> GetModifiedAsync(string filter)
        {
            string query = string.Format(
                "/xml/modified.xml?timestamp=0&filter={0}&password={1}&mode=zip", filter, _config.Password);
            return QueryAsync(query);
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
