using System;
using System.Threading.Tasks;
using Apfelmus.Avalonia.Services;
using ApfelmusFramework.Classes.Logic;
using ApfelmusFramework.Classes.Serializer;
using Config = ApfelmusFramework.Classes.Config.Config;

namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Login-/Verbindungsdialog. Nutzt exakt dieselbe Kernbibliothek wie der WPF-Client:
    /// Passwort wird per <see cref="CreateMd5Hash"/> als MD5-Hex abgelegt (Wire-Vorgabe des Cores),
    /// die Verbindung per <see cref="CoreClient"/>/WebConnect geprueft und die
    /// <see cref="Config"/> per <see cref="ConfigSerializer"/> als Config.xml gespeichert.
    /// </summary>
    public sealed class LoginViewModel : ViewModelBase
    {
        private string _host = "localhost";
        private string _port = "9851";
        private string _password = string.Empty;
        private bool _useCompression = true;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public LoginViewModel()
        {
            TryLoadExistingConfig();
            LoginCommand = new RelayCommand(LoginAsync, () => !IsBusy);
        }

        public event Action<Config>? LoginSucceeded;

        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool UseCompression
        {
            get => _useCompression;
            set => SetProperty(ref _useCompression, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand LoginCommand { get; }

        private void TryLoadExistingConfig()
        {
            try
            {
                var existing = ConfigSerializer.DeserializeFromFile();
                if (existing != null)
                {
                    _host = string.IsNullOrEmpty(existing.HostName) ? _host : existing.HostName;
                    _port = existing.Port > 0 ? existing.Port.ToString() : _port;
                    _useCompression = existing.UseCompression;
                }
            }
            catch (Exception)
            {
                // Erster Start / keine Config - Defaults verwenden.
            }
        }

        private async Task LoginAsync()
        {
            if (!int.TryParse(Port, out int port))
            {
                StatusMessage = "Ungueltiger Port.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Verbinde...";

            var config = new Config
            {
                HostName = Host,
                Port = port,
                Password = CreateMd5Hash.GetMD5Hash(Password),
                UseCompression = UseCompression,
                RefreshRate = 1500,
                Theme = ThemeNames.Dark,
            };

            var client = new CoreClient(config);
            bool reachable = await client.CheckConnectionAsync();
            if (!reachable)
            {
                StatusMessage = "Core nicht erreichbar. Host/Port pruefen.";
                IsBusy = false;
                return;
            }

            try
            {
                ConfigSerializer.SerializeToFile(config);
            }
            catch (Exception ex)
            {
                StatusMessage = "Config konnte nicht gespeichert werden: " + ex.Message;
            }

            IsBusy = false;
            LoginSucceeded?.Invoke(config);
        }
    }

    /// <summary>Theme-Namen (dupliziert bewusst die WPF-Konstanten; UI-Framework-unabhaengig gehalten).</summary>
    public static class ThemeNames
    {
        public const string Dark = "Dark";
        public const string Light = "Light";
    }
}
