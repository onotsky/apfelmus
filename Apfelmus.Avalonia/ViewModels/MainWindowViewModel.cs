using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Apfelmus.Avalonia.Services;
using ApfelmusFramework.Classes.Modified;
using Config = ApfelmusFramework.Classes.Config.Config;

namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Hauptfenster-ViewModel. Pollt zyklisch den Core (ueber <see cref="CoreClient"/> und damit die
    /// gemeinsame Kernbibliothek) und fuellt die Anzeige des Start-/Uebersichts-Tabs sowie - als
    /// Demonstration der Wiederverwendung - die Download-/Upload-Listen.
    ///
    /// Grundgeruest: Start-Tab + Download-/Upload-Sammlungen sind verdrahtet. Suche, Share, Server
    /// und das Partlisten-Rendering sind noch als Platzhalter offen (siehe README.md).
    /// </summary>
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly Config _config;
        private readonly CoreClient _client;
        private readonly DispatcherTimer _timer;

        private string _connectionStatus = "Verbinde...";
        private string _coreVersion = "-";
        private int _users;
        private int _files;
        private string _fileSize = "-";
        private bool _firewalled;
        private string _ownIp = "-";

        public MainWindowViewModel(Config config)
        {
            _config = config;
            _client = new CoreClient(config);

            Downloads = new ObservableCollection<Download>();
            Uploads = new ObservableCollection<Upload>();

            int interval = config.RefreshRate > 0 ? config.RefreshRate : 1500;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(interval) };
            _timer.Tick += async (_, _) => await PollAsync();
            _timer.Start();

            // Sofort einmal laden, nicht erst nach dem ersten Intervall.
            _ = PollAsync();
        }

        public ObservableCollection<Download> Downloads { get; }

        public ObservableCollection<Upload> Uploads { get; }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set => SetProperty(ref _connectionStatus, value);
        }

        public string CoreVersion
        {
            get => _coreVersion;
            private set => SetProperty(ref _coreVersion, value);
        }

        public int Users
        {
            get => _users;
            private set => SetProperty(ref _users, value);
        }

        public int Files
        {
            get => _files;
            private set => SetProperty(ref _files, value);
        }

        public string FileSize
        {
            get => _fileSize;
            private set => SetProperty(ref _fileSize, value);
        }

        public bool Firewalled
        {
            get => _firewalled;
            private set => SetProperty(ref _firewalled, value);
        }

        public string OwnIp
        {
            get => _ownIp;
            private set => SetProperty(ref _ownIp, value);
        }

        private async System.Threading.Tasks.Task PollAsync()
        {
            var info = await _client.GetInformationAsync();
            if (info == null)
            {
                ConnectionStatus = "Keine Verbindung zum Core";
                return;
            }

            ConnectionStatus = "Verbunden";

            if (info.GeneralInformation != null)
            {
                CoreVersion = info.GeneralInformation.Version ?? "-";
            }

            if (info.NetworkInfo != null)
            {
                Users = info.NetworkInfo.Users;
                Files = info.NetworkInfo.Files;
                FileSize = info.NetworkInfo.FileSize ?? "-";
                Firewalled = info.NetworkInfo.Firewalled;
                OwnIp = info.NetworkInfo.Ip ?? "-";
            }

            await RefreshTransfersAsync();
        }

        private async System.Threading.Tasks.Task RefreshTransfersAsync()
        {
            var down = await _client.GetModifiedAsync("down");
            if (down?.Download != null)
            {
                Downloads.Clear();
                foreach (var d in down.Download)
                {
                    Downloads.Add(d);
                }
            }

            var up = await _client.GetModifiedAsync("uploads");
            if (up?.Upload != null)
            {
                Uploads.Clear();
                foreach (var u in up.Upload)
                {
                    Uploads.Add(u);
                }
            }
        }
    }
}
