using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using Apfelmus.Avalonia.Services;
using ApfelmusFramework.Classes.Modified;
using ApfelmusFramework.Classes.Serializer;
using ApfelmusFramework.Classes.Logic;
using ShareItem = ApfelmusFramework.Classes.Share.Share;
using Config = ApfelmusFramework.Classes.Config.Config;

namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Hauptfenster-ViewModel. Pollt zyklisch den Core (ueber <see cref="CoreClient"/> und damit die
    /// gemeinsame Kernbibliothek) und fuellt alle Bereiche: Start/Uebersicht, Downloads, Uploads,
    /// Server, Suche, Mein Share sowie Einstellungen. Kommandos (Abbrechen/Pause/Fortsetzen,
    /// Verbinden, Suchen, Download starten, Info-URL oeffnen) rufen die /function/*-Endpunkte auf.
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

        private Download? _selectedDownload;
        private Server? _selectedServer;
        private SearchEntry? _selectedSearchResult;
        private string _searchText = string.Empty;
        private bool _searchActive;

        private int _refreshRate;
        private string _releaseInfoHost = string.Empty;
        private string _settingsStatus = string.Empty;

        public MainWindowViewModel(Config config)
        {
            _config = config;
            _client = new CoreClient(config);

            Downloads = new ObservableCollection<Download>();
            Uploads = new ObservableCollection<Upload>();
            Servers = new ObservableCollection<Server>();
            SearchResults = new ObservableCollection<SearchEntry>();
            Shares = new ObservableCollection<ShareItem>();

            _refreshRate = config.RefreshRate > 0 ? config.RefreshRate : 1500;
            _releaseInfoHost = string.IsNullOrWhiteSpace(config.ReleaseInfoHost)
                ? ReleaseInfo.DefaultHost
                : config.ReleaseInfoHost;

            CancelDownloadCommand = new RelayCommand(CancelDownloadAsync, () => SelectedDownload != null);
            PauseDownloadCommand = new RelayCommand(PauseDownloadAsync, () => SelectedDownload != null);
            ResumeDownloadCommand = new RelayCommand(ResumeDownloadAsync, () => SelectedDownload != null);
            ReleaseInfoDownloadCommand = new RelayCommand(ReleaseInfoDownloadAsync, () => SelectedDownload != null);
            ConnectServerCommand = new RelayCommand(ConnectServerAsync, () => SelectedServer != null);
            StartSearchCommand = new RelayCommand(StartSearchAsync, () => !string.IsNullOrWhiteSpace(SearchText));
            DownloadSearchResultCommand = new RelayCommand(DownloadSearchResultAsync, () => SelectedSearchResult != null);
            ReleaseInfoSearchCommand = new RelayCommand(ReleaseInfoSearchAsync, () => SelectedSearchResult != null);
            SaveSettingsCommand = new RelayCommand(SaveSettingsAsync);
            ApplyDarkThemeCommand = new RelayCommand(() => ApplyThemeAsync(ThemeNames.Dark));
            ApplyLightThemeCommand = new RelayCommand(() => ApplyThemeAsync(ThemeNames.Light));

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_refreshRate) };
            _timer.Tick += async (_, _) => await PollAsync();
            _timer.Start();

            _ = PollAsync();
        }

        public ObservableCollection<Download> Downloads { get; }
        public ObservableCollection<Upload> Uploads { get; }
        public ObservableCollection<Server> Servers { get; }
        public ObservableCollection<SearchEntry> SearchResults { get; }
        public ObservableCollection<ShareItem> Shares { get; }

        public RelayCommand CancelDownloadCommand { get; }
        public RelayCommand PauseDownloadCommand { get; }
        public RelayCommand ResumeDownloadCommand { get; }
        public RelayCommand ReleaseInfoDownloadCommand { get; }
        public RelayCommand ConnectServerCommand { get; }
        public RelayCommand StartSearchCommand { get; }
        public RelayCommand DownloadSearchResultCommand { get; }
        public RelayCommand ReleaseInfoSearchCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand ApplyDarkThemeCommand { get; }
        public RelayCommand ApplyLightThemeCommand { get; }

        public string ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
        public string CoreVersion { get => _coreVersion; private set => SetProperty(ref _coreVersion, value); }
        public int Users { get => _users; private set => SetProperty(ref _users, value); }
        public int Files { get => _files; private set => SetProperty(ref _files, value); }
        public string FileSize { get => _fileSize; private set => SetProperty(ref _fileSize, value); }
        public bool Firewalled { get => _firewalled; private set => SetProperty(ref _firewalled, value); }
        public string OwnIp { get => _ownIp; private set => SetProperty(ref _ownIp, value); }

        public Download? SelectedDownload
        {
            get => _selectedDownload;
            set
            {
                if (SetProperty(ref _selectedDownload, value))
                {
                    CancelDownloadCommand.RaiseCanExecuteChanged();
                    PauseDownloadCommand.RaiseCanExecuteChanged();
                    ResumeDownloadCommand.RaiseCanExecuteChanged();
                    ReleaseInfoDownloadCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public Server? SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (SetProperty(ref _selectedServer, value))
                {
                    ConnectServerCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public SearchEntry? SelectedSearchResult
        {
            get => _selectedSearchResult;
            set
            {
                if (SetProperty(ref _selectedSearchResult, value))
                {
                    DownloadSearchResultCommand.RaiseCanExecuteChanged();
                    ReleaseInfoSearchCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    StartSearchCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int RefreshRate { get => _refreshRate; set => SetProperty(ref _refreshRate, value); }
        public string ReleaseInfoHost { get => _releaseInfoHost; set => SetProperty(ref _releaseInfoHost, value); }
        public string SettingsStatus { get => _settingsStatus; private set => SetProperty(ref _settingsStatus, value); }

        // ---- Polling ---------------------------------------------------------------------------

        private async Task PollAsync()
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

            await RefreshDownloadsAsync();
            await RefreshUploadsAsync();
            await RefreshServersAsync();
            await RefreshSharesAsync();

            if (_searchActive)
            {
                await RefreshSearchResultsAsync();
            }
        }

        private async Task RefreshDownloadsAsync()
        {
            var result = await _client.GetModifiedAsync("down");
            if (result?.Download != null)
            {
                MergeById(Downloads, result.Download, d => d.Id, (target, src) =>
                {
                    target.FileName = src.FileName;
                    target.Size = src.Size;
                    target.Status = src.Status;
                    target.Speed = src.Speed;
                    target.ActiveUsers = src.ActiveUsers;
                    target.AllUsers = src.AllUsers;
                    target.Percentages = src.Percentages;
                });
            }
        }

        private async Task RefreshUploadsAsync()
        {
            var result = await _client.GetModifiedAsync("uploads");
            if (result?.Upload != null)
            {
                Uploads.Clear();
                foreach (var u in result.Upload)
                {
                    Uploads.Add(u);
                }
            }
        }

        private async Task RefreshServersAsync()
        {
            var result = await _client.GetModifiedAsync("server");
            if (result?.Server != null)
            {
                MergeById(Servers, result.Server, s => s.Id, (target, src) =>
                {
                    target.Name = src.Name;
                    target.Host = src.Host;
                    target.Port = src.Port;
                    target.ConnectionTry = src.ConnectionTry;
                });
            }
        }

        private async Task RefreshSharesAsync()
        {
            var result = await _client.GetShareAsync();
            if (result?.Shares?.Share != null)
            {
                Shares.Clear();
                foreach (var s in result.Shares.Share)
                {
                    Shares.Add(s);
                }
            }
        }

        private async Task RefreshSearchResultsAsync()
        {
            var result = await _client.GetModifiedAsync("search");
            if (result?.SearchEntry != null)
            {
                foreach (var entry in result.SearchEntry)
                {
                    if (!SearchResults.Any(e => e.Id == entry.Id))
                    {
                        SearchResults.Add(entry);
                    }
                }
            }
        }

        // ---- Commands --------------------------------------------------------------------------

        private async Task CancelDownloadAsync()
        {
            if (SelectedDownload != null)
            {
                await _client.CancelDownloadAsync(SelectedDownload.Id);
                await RefreshDownloadsAsync();
            }
        }

        private async Task PauseDownloadAsync()
        {
            if (SelectedDownload != null)
            {
                await _client.PauseDownloadAsync(SelectedDownload.Id);
            }
        }

        private async Task ResumeDownloadAsync()
        {
            if (SelectedDownload != null)
            {
                await _client.ResumeDownloadAsync(SelectedDownload.Id);
            }
        }

        private Task ReleaseInfoDownloadAsync()
        {
            if (SelectedDownload != null)
            {
                OpenReleaseInfo(SelectedDownload.FileName, SelectedDownload.Hash, SelectedDownload.Size);
            }

            return Task.CompletedTask;
        }

        private async Task ConnectServerAsync()
        {
            if (SelectedServer != null)
            {
                await _client.ConnectServerAsync(SelectedServer.Id);
            }
        }

        private async Task StartSearchAsync()
        {
            SearchResults.Clear();
            _searchActive = true;
            await _client.StartSearchAsync(SearchText);
            // Ergebnisse trudeln beim naechsten Poll ein.
        }

        private async Task DownloadSearchResultAsync()
        {
            if (SelectedSearchResult != null)
            {
                await _client.StartDownloadAsync(
                    SelectedSearchResult.FileName?.Name ?? string.Empty,
                    SelectedSearchResult.Checksum,
                    SelectedSearchResult.Size.ToString());
            }
        }

        private Task ReleaseInfoSearchAsync()
        {
            if (SelectedSearchResult != null)
            {
                OpenReleaseInfo(
                    SelectedSearchResult.FileName?.Name,
                    SelectedSearchResult.Checksum,
                    SelectedSearchResult.Size.ToString());
            }

            return Task.CompletedTask;
        }

        private Task SaveSettingsAsync()
        {
            try
            {
                _config.RefreshRate = RefreshRate > 0 ? RefreshRate : 1500;
                _config.ReleaseInfoHost = string.IsNullOrWhiteSpace(ReleaseInfoHost)
                    ? ReleaseInfo.DefaultHost
                    : ReleaseInfoHost.Trim();
                ConfigSerializer.SerializeToFile(_config);

                _timer.Interval = TimeSpan.FromMilliseconds(_config.RefreshRate);
                SettingsStatus = "Gespeichert.";
            }
            catch (Exception ex)
            {
                SettingsStatus = "Fehler: " + ex.Message;
            }

            return Task.CompletedTask;
        }

        private Task ApplyThemeAsync(string theme)
        {
            var variant = string.Equals(theme, ThemeNames.Light, StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = variant;
            }

            try
            {
                _config.Theme = theme;
                ConfigSerializer.SerializeToFile(_config);
            }
            catch (Exception)
            {
                // Persistenz-Fehler ignorieren; Theme ist dennoch angewandt.
            }

            return Task.CompletedTask;
        }

        // ---- Helpers ---------------------------------------------------------------------------

        private void OpenReleaseInfo(string? fileName, string? hash, string? size)
        {
            string host = string.IsNullOrWhiteSpace(_config.ReleaseInfoHost)
                ? ReleaseInfo.DefaultHost
                : _config.ReleaseInfoHost;
            ReleaseInfo.Open(host, fileName, hash, size);
        }

        /// <summary>
        /// Aktualisiert eine ObservableCollection anhand einer Id-Zuordnung, ohne sie komplett neu
        /// aufzubauen: vorhandene Eintraege werden aktualisiert, fehlende entfernt, neue angehaengt.
        /// Erhaelt so Auswahl und Scrollposition im DataGrid.
        /// </summary>
        private static void MergeById<T, TKey>(
            ObservableCollection<T> target,
            IEnumerable<T> source,
            Func<T, TKey> keySelector,
            Action<T, T> update)
            where TKey : notnull
        {
            var incoming = source.ToList();
            var incomingKeys = new HashSet<TKey>(incoming.Select(keySelector));

            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!incomingKeys.Contains(keySelector(target[i])))
                {
                    target.RemoveAt(i);
                }
            }

            var existing = target.ToDictionary(keySelector);
            foreach (var item in incoming)
            {
                if (existing.TryGetValue(keySelector(item), out var current))
                {
                    update(current, item);
                }
                else
                {
                    target.Add(item);
                }
            }
        }
    }
}
