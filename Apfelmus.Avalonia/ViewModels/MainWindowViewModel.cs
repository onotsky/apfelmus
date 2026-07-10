using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
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
    /// Hauptfenster-ViewModel. Pollt zyklisch den Core (gemeinsame Kernbibliothek) und deckt die
    /// Bereiche des WPF-Clients ab: Start/Uebersicht, Downloads (inkl. Quellen), Uploads, Suche,
    /// Server, Mein Share, Statusleiste sowie Menue-Aktionen (Core beenden, Theme) und AJLink-Transfer.
    /// </summary>
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly Config _config;
        private readonly CoreClient _client;
        private readonly DispatcherTimer _timer;
        private List<User> _allUsers = new();

        public MainWindowViewModel(Config config)
        {
            _config = config;
            _client = new CoreClient(config);

            Downloads = new ObservableCollection<Download>();
            DownloadSources = new ObservableCollection<User>();
            Uploads = new ObservableCollection<Upload>();
            Servers = new ObservableCollection<Server>();
            SearchResults = new ObservableCollection<SearchEntry>();
            Shares = new ObservableCollection<ShareItem>();

            PowerValues = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5 };
            PriorityValues = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            _selectedPowerValue = 0;
            _selectedPriority = 1;

            _guiVersion = Assembly.GetExecutingAssembly().GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "-";
            _refreshRate = config.RefreshRate > 0 ? config.RefreshRate : 1500;
            _releaseInfoHost = string.IsNullOrWhiteSpace(config.ReleaseInfoHost) ? ReleaseInfo.DefaultHost : config.ReleaseInfoHost;

            // AJLink / Menue
            TransferAjLinkCommand = new RelayCommand(TransferAjLinkAsync, () => !string.IsNullOrWhiteSpace(AjLinkText));
            CoreExitCommand = new RelayCommand(() => _client.ExitCoreAsync());
            ApplyDarkThemeCommand = new RelayCommand(() => ApplyTheme(ThemeNames.Dark));
            ApplyLightThemeCommand = new RelayCommand(() => ApplyTheme(ThemeNames.Light));
            GermanCommand = new RelayCommand(() => ApplyLanguage("de"));
            EnglishCommand = new RelayCommand(() => ApplyLanguage("en"));
            ItalianCommand = new RelayCommand(() => ApplyLanguage("it"));
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            LoadCoreSettingsCommand = new RelayCommand(LoadCoreSettingsAsync);
            SaveCoreSettingsCommand = new RelayCommand(SaveCoreSettingsAsync);
            ChangePasswordCommand = new RelayCommand(ChangePasswordAsync, () => !string.IsNullOrEmpty(NewPassword));

            // Downloads
            ContinueCommand = new RelayCommand(async () => await WithDownload(id => _client.ResumeDownloadAsync(id)), () => SelectedDownload != null);
            BreakCommand = new RelayCommand(async () => await WithDownload(id => _client.PauseDownloadAsync(id)), () => SelectedDownload != null);
            CancelCommand = new RelayCommand(async () => { await WithDownload(id => _client.CancelDownloadAsync(id)); await RefreshDownloadsAsync(); }, () => SelectedDownload != null);
            CleanFinishedCommand = new RelayCommand(async () => { await _client.CleanDownloadListAsync(); await RefreshDownloadsAsync(); });
            ApplyPowerDownloadCommand = new RelayCommand(async () => await WithDownload(id => _client.SetPowerDownloadAsync(id, SelectedPowerValue)), () => SelectedDownload != null);
            SetDownloadPriorityCommand = new RelayCommand(async () => await WithDownload(id => _client.SetDownloadPriorityAsync(id, SelectedPriority)), () => SelectedDownload != null);
            ReleaseInfoDownloadCommand = new RelayCommand(() => { if (SelectedDownload != null) OpenReleaseInfo(SelectedDownload.FileName, SelectedDownload.Hash, SelectedDownload.Size); return Task.CompletedTask; }, () => SelectedDownload != null);
            CopyDownloadLinkCommand = new RelayCommand(() => { if (SelectedDownload != null) RaiseCopy(BuildAjfsp(SelectedDownload.FileName, SelectedDownload.Hash, SelectedDownload.Size)); return Task.CompletedTask; }, () => SelectedDownload != null);

            // Server
            ConnectServerCommand = new RelayCommand(async () => { if (SelectedServer != null) await _client.ConnectServerAsync(SelectedServer.Id); }, () => SelectedServer != null);
            RemoveServerCommand = new RelayCommand(async () => { if (SelectedServer != null) { await _client.RemoveServerAsync(SelectedServer.Id); await RefreshServersAsync(); } }, () => SelectedServer != null);
            AddOfficialServersCommand = new RelayCommand(() => _client.AddOfficialServersAsync());

            // Suche
            StartSearchCommand = new RelayCommand(StartSearchAsync, () => !string.IsNullOrWhiteSpace(SearchText));
            StopSearchCommand = new RelayCommand(StopSearchAsync, () => SearchRunning);
            DownloadSearchResultCommand = new RelayCommand(async () => { if (SelectedSearchResult != null) await _client.StartDownloadAsync(SelectedSearchResult.FileName?.Name ?? string.Empty, SelectedSearchResult.Checksum, SelectedSearchResult.Size.ToString()); }, () => SelectedSearchResult != null);
            ReleaseInfoSearchCommand = new RelayCommand(() => { if (SelectedSearchResult != null) OpenReleaseInfo(SelectedSearchResult.FileName?.Name, SelectedSearchResult.Checksum, SelectedSearchResult.Size.ToString()); return Task.CompletedTask; }, () => SelectedSearchResult != null);
            CopySearchLinkCommand = new RelayCommand(() => { if (SelectedSearchResult != null) RaiseCopy(BuildAjfsp(SelectedSearchResult.FileName?.Name, SelectedSearchResult.Checksum, SelectedSearchResult.Size.ToString())); return Task.CompletedTask; }, () => SelectedSearchResult != null);

            // Share
            SetSharePriorityCommand = new RelayCommand(async () => { if (SelectedShare != null) { await _client.SetDownloadPriorityAsync(SelectedShare.Id, SelectedPriority); } }, () => SelectedShare != null);
            DelSharePriorityCommand = new RelayCommand(async () => { if (SelectedShare != null) { await _client.SetDownloadPriorityAsync(SelectedShare.Id, 1); } }, () => SelectedShare != null);
            ReleaseInfoShareCommand = new RelayCommand(() => { if (SelectedShare != null) OpenReleaseInfo(SelectedShare.ShortFileName, SelectedShare.CheckSum, SelectedShare.Size.ToString()); return Task.CompletedTask; }, () => SelectedShare != null);
            CopyShareLinkCommand = new RelayCommand(() => { if (SelectedShare != null) RaiseCopy(BuildAjfsp(SelectedShare.ShortFileName, SelectedShare.CheckSum, SelectedShare.Size.ToString())); return Task.CompletedTask; }, () => SelectedShare != null);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_refreshRate) };
            _timer.Tick += async (_, _) => await PollAsync();
            _timer.Start();
            _ = PollAsync();
            _ = LoadCoreSettingsAsync();

            // Gespeicherte Sprache anwenden.
            LanguageManager.Apply(LanguageManager.Normalize(config.LanguageFile));
        }

        /// <summary>Wird von der View gesetzt/abonniert, um Text in die Zwischenablage zu legen.</summary>
        public event Action<string>? CopyRequested;

        // ---- Collections ----
        public ObservableCollection<Download> Downloads { get; }
        public ObservableCollection<User> DownloadSources { get; }
        public ObservableCollection<Upload> Uploads { get; }
        public ObservableCollection<Server> Servers { get; }
        public ObservableCollection<SearchEntry> SearchResults { get; }
        public ObservableCollection<ShareItem> Shares { get; }
        public ObservableCollection<int> PowerValues { get; }
        public ObservableCollection<int> PriorityValues { get; }

        // ---- Commands ----
        public RelayCommand TransferAjLinkCommand { get; }
        public RelayCommand CoreExitCommand { get; }
        public RelayCommand ApplyDarkThemeCommand { get; }
        public RelayCommand ApplyLightThemeCommand { get; }
        public RelayCommand GermanCommand { get; }
        public RelayCommand EnglishCommand { get; }
        public RelayCommand ItalianCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand LoadCoreSettingsCommand { get; }
        public RelayCommand SaveCoreSettingsCommand { get; }
        public RelayCommand ChangePasswordCommand { get; }
        public RelayCommand ContinueCommand { get; }
        public RelayCommand BreakCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand CleanFinishedCommand { get; }
        public RelayCommand ApplyPowerDownloadCommand { get; }
        public RelayCommand SetDownloadPriorityCommand { get; }
        public RelayCommand ReleaseInfoDownloadCommand { get; }
        public RelayCommand CopyDownloadLinkCommand { get; }
        public RelayCommand ConnectServerCommand { get; }
        public RelayCommand RemoveServerCommand { get; }
        public RelayCommand AddOfficialServersCommand { get; }
        public RelayCommand StartSearchCommand { get; }
        public RelayCommand StopSearchCommand { get; }
        public RelayCommand DownloadSearchResultCommand { get; }
        public RelayCommand ReleaseInfoSearchCommand { get; }
        public RelayCommand CopySearchLinkCommand { get; }
        public RelayCommand SetSharePriorityCommand { get; }
        public RelayCommand DelSharePriorityCommand { get; }
        public RelayCommand ReleaseInfoShareCommand { get; }
        public RelayCommand CopyShareLinkCommand { get; }

        // ---- Start / Uebersicht ----
        private string _connectionStatus = "Verbinde...";
        private string _guiVersion;
        private string _coreVersion = "-";
        private int _users, _files, _openConnections, _uploadQueue;
        private string _fileSize = "-", _ownIp = "-", _serverName = "-", _connectedSince = "-", _welcomeMessage = "";
        private bool _firewalled;

        public string ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
        public string GuiVersion { get => _guiVersion; private set => SetProperty(ref _guiVersion, value); }
        public string CoreVersion { get => _coreVersion; private set => SetProperty(ref _coreVersion, value); }
        public int Users { get => _users; private set => SetProperty(ref _users, value); }
        public int Files { get => _files; private set => SetProperty(ref _files, value); }
        public int OpenConnections { get => _openConnections; private set => SetProperty(ref _openConnections, value); }
        public int UploadQueue { get => _uploadQueue; private set => SetProperty(ref _uploadQueue, value); }
        public string FileSize { get => _fileSize; private set => SetProperty(ref _fileSize, value); }
        public string OwnIp { get => _ownIp; private set => SetProperty(ref _ownIp, value); }
        public string ServerName { get => _serverName; private set => SetProperty(ref _serverName, value); }
        public string ConnectedSince { get => _connectedSince; private set => SetProperty(ref _connectedSince, value); }
        public string WelcomeMessage { get => _welcomeMessage; private set => SetProperty(ref _welcomeMessage, value); }
        public bool Firewalled { get => _firewalled; private set => SetProperty(ref _firewalled, value); }

        // ---- Statusleiste ----
        private long _credits, _sessionUpload, _sessionDownload;
        private int _uploadSpeed, _downloadSpeed;
        public long Credits { get => _credits; private set => SetProperty(ref _credits, value); }
        public long SessionUpload { get => _sessionUpload; private set => SetProperty(ref _sessionUpload, value); }
        public long SessionDownload { get => _sessionDownload; private set => SetProperty(ref _sessionDownload, value); }
        public int UploadSpeed { get => _uploadSpeed; private set => SetProperty(ref _uploadSpeed, value); }
        public int DownloadSpeed { get => _downloadSpeed; private set => SetProperty(ref _downloadSpeed, value); }

        // ---- Auswahl / Eingaben ----
        private Download? _selectedDownload;
        private Server? _selectedServer;
        private SearchEntry? _selectedSearchResult;
        private ShareItem? _selectedShare;
        private string _searchText = string.Empty;
        private string _ajLinkText = string.Empty;
        private int _selectedPowerValue, _selectedPriority;
        private int _foundFiles, _sumSearches, _currentSearchId = -1;
        private bool _searchRunning;
        private int _refreshRate;
        private string _releaseInfoHost, _settingsStatus = string.Empty;

        public Download? SelectedDownload
        {
            get => _selectedDownload;
            set { if (SetProperty(ref _selectedDownload, value)) { UpdateDownloadSources(); RaiseDownloadCmds(); } }
        }
        public Server? SelectedServer
        {
            get => _selectedServer;
            set { if (SetProperty(ref _selectedServer, value)) { ConnectServerCommand.RaiseCanExecuteChanged(); RemoveServerCommand.RaiseCanExecuteChanged(); } }
        }
        public SearchEntry? SelectedSearchResult
        {
            get => _selectedSearchResult;
            set { if (SetProperty(ref _selectedSearchResult, value)) { DownloadSearchResultCommand.RaiseCanExecuteChanged(); ReleaseInfoSearchCommand.RaiseCanExecuteChanged(); CopySearchLinkCommand.RaiseCanExecuteChanged(); } }
        }
        public ShareItem? SelectedShare
        {
            get => _selectedShare;
            set { if (SetProperty(ref _selectedShare, value)) { SetSharePriorityCommand.RaiseCanExecuteChanged(); DelSharePriorityCommand.RaiseCanExecuteChanged(); ReleaseInfoShareCommand.RaiseCanExecuteChanged(); CopyShareLinkCommand.RaiseCanExecuteChanged(); } }
        }
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) StartSearchCommand.RaiseCanExecuteChanged(); } }
        public string AjLinkText { get => _ajLinkText; set { if (SetProperty(ref _ajLinkText, value)) TransferAjLinkCommand.RaiseCanExecuteChanged(); } }
        public int SelectedPowerValue { get => _selectedPowerValue; set => SetProperty(ref _selectedPowerValue, value); }
        public int SelectedPriority { get => _selectedPriority; set => SetProperty(ref _selectedPriority, value); }
        public int FoundFiles { get => _foundFiles; private set => SetProperty(ref _foundFiles, value); }
        public int SumSearches { get => _sumSearches; private set => SetProperty(ref _sumSearches, value); }
        public bool SearchRunning { get => _searchRunning; private set { if (SetProperty(ref _searchRunning, value)) StopSearchCommand.RaiseCanExecuteChanged(); } }
        public int RefreshRate { get => _refreshRate; set => SetProperty(ref _refreshRate, value); }
        public string ReleaseInfoHost { get => _releaseInfoHost; set => SetProperty(ref _releaseInfoHost, value); }
        public string SettingsStatus { get => _settingsStatus; private set => SetProperty(ref _settingsStatus, value); }

        // ---- Core-Einstellungen (settings.xml) ----
        private string _coreNick = string.Empty, _coreIncomingDir = string.Empty, _coreTempDir = string.Empty;
        private string _newPassword = string.Empty, _coreSettingsStatus = string.Empty;
        private int _corePort, _coreXmlPort, _coreMaxConnections, _coreMaxUpload, _coreMaxDownload, _coreSpeedPerSlot, _coreMaxNewConn, _coreMaxSources;
        private bool _coreAutoConnect;

        public string CoreNick { get => _coreNick; set => SetProperty(ref _coreNick, value); }
        public string CoreIncomingDir { get => _coreIncomingDir; set => SetProperty(ref _coreIncomingDir, value); }
        public string CoreTempDir { get => _coreTempDir; set => SetProperty(ref _coreTempDir, value); }
        public int CorePort { get => _corePort; set => SetProperty(ref _corePort, value); }
        public int CoreXmlPort { get => _coreXmlPort; set => SetProperty(ref _coreXmlPort, value); }
        public int CoreMaxConnections { get => _coreMaxConnections; set => SetProperty(ref _coreMaxConnections, value); }
        public int CoreMaxUpload { get => _coreMaxUpload; set => SetProperty(ref _coreMaxUpload, value); }
        public int CoreMaxDownload { get => _coreMaxDownload; set => SetProperty(ref _coreMaxDownload, value); }
        public int CoreSpeedPerSlot { get => _coreSpeedPerSlot; set => SetProperty(ref _coreSpeedPerSlot, value); }
        public int CoreMaxNewConnectionsPerTurn { get => _coreMaxNewConn; set => SetProperty(ref _coreMaxNewConn, value); }
        public int CoreMaxSourcesPerFile { get => _coreMaxSources; set => SetProperty(ref _coreMaxSources, value); }
        public bool CoreAutoConnect { get => _coreAutoConnect; set => SetProperty(ref _coreAutoConnect, value); }
        public string NewPassword { get => _newPassword; set { if (SetProperty(ref _newPassword, value)) ChangePasswordCommand.RaiseCanExecuteChanged(); } }
        public string CoreSettingsStatus { get => _coreSettingsStatus; private set => SetProperty(ref _coreSettingsStatus, value); }

        // ---- Partlisten-Verfuegbarkeitsbalken ----
        private WriteableBitmap? _partlistImage;
        public WriteableBitmap? PartlistImage { get => _partlistImage; private set => SetProperty(ref _partlistImage, value); }

        // ---- Polling ----
        private async Task PollAsync()
        {
            var info = await _client.GetInformationAsync();
            if (info == null) { ConnectionStatus = "Keine Verbindung zum Core"; return; }
            ConnectionStatus = "Verbunden";

            if (info.GeneralInformation != null) CoreVersion = info.GeneralInformation.Version ?? "-";
            if (info.NetworkInfo != null)
            {
                var n = info.NetworkInfo;
                Users = n.Users; Files = n.Files; FileSize = n.FileSize ?? "-";
                Firewalled = n.Firewalled; OwnIp = n.Ip ?? "-";
                WelcomeMessage = n.WelcomeMessage ?? "";
                ConnectedSince = n.ConnectedSince > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(n.ConnectedSince).LocalDateTime.ToString("g")
                    : "-";
                _connectedServerId = n.ConnectedWithServerId;
            }

            var infos = await _client.GetInformationsAsync();
            if (infos?.Information is { } d)
            {
                Credits = d.Credits; UploadSpeed = d.UploadSpeed; DownloadSpeed = d.DownloadSpeed;
                SessionUpload = d.SessionUpload; SessionDownload = d.SessionDownload;
                OpenConnections = d.OpenConnections; UploadQueue = d.MaxUploadPositions;
            }

            await RefreshDownloadsAsync();
            await RefreshUsersAsync();
            await RefreshUploadsAsync();
            await RefreshServersAsync();
            await RefreshSharesAsync();
            if (_currentSearchId >= 0) await RefreshSearchAsync();
        }

        private int _connectedServerId;

        private async Task RefreshDownloadsAsync()
        {
            var r = await _client.GetModifiedAsync("down");
            if (r?.Download != null)
                MergeById(Downloads, r.Download, x => x.Id, (t, s) =>
                {
                    t.FileName = s.FileName; t.Size = s.Size; t.Status = s.Status; t.Speed = s.Speed;
                    t.ActiveUsers = s.ActiveUsers; t.AllUsers = s.AllUsers; t.Percentages = s.Percentages;
                    t.DownloadedFilesize = s.DownloadedFilesize; t.DownloadRest = s.DownloadRest;
                    t.TimeToEnd = s.TimeToEnd; t.PowerDownload = s.PowerDownload;
                });
        }

        private async Task RefreshUsersAsync()
        {
            var r = await _client.GetModifiedAsync("user");
            if (r?.User != null)
            {
                _allUsers = r.User.ToList();
                UpdateDownloadSources();
            }
        }

        private void UpdateDownloadSources()
        {
            DownloadSources.Clear();
            if (SelectedDownload == null) { PartlistImage = null; return; }
            foreach (var u in _allUsers.Where(u => u.DownloadId == SelectedDownload.Id))
                DownloadSources.Add(u);
            _ = BuildPartlistAsync();
        }

        /// <summary>Holt die Partliste des gewaehlten Downloads und rendert den Verfuegbarkeitsbalken.</summary>
        private async Task BuildPartlistAsync()
        {
            var sel = SelectedDownload;
            if (sel == null) { PartlistImage = null; return; }

            var pl = await _client.GetDownloadPartlistAsync(sel.Id);
            // Auswahl koennte sich waehrend des Ladens geaendert haben.
            if (SelectedDownload != sel) return;
            if (pl?.FileInformation == null || pl.Parts == null) { PartlistImage = null; return; }

            var sources = DownloadSources.Where(u => u.Status == 2 || u.ActualDownloadPosition > u.DownloadFrom).ToList();
            PartlistImage = PartlistRenderer.Render(pl.FileInformation.Filesize, pl.Parts, sources, 240, 4);
        }

        private async Task RefreshUploadsAsync()
        {
            var r = await _client.GetModifiedAsync("uploads");
            if (r?.Upload != null)
            {
                Uploads.Clear();
                foreach (var u in r.Upload) Uploads.Add(u);
            }
        }

        private async Task RefreshServersAsync()
        {
            var r = await _client.GetModifiedAsync("server");
            if (r?.Server != null)
                MergeById(Servers, r.Server, x => x.Id, (t, s) =>
                {
                    t.Name = s.Name; t.Host = s.Host; t.Port = s.Port; t.LastSeen = s.LastSeen; t.ConnectionTry = s.ConnectionTry;
                });

            ServerName = "-";
            foreach (var s in Servers)
            {
                s.IsConnected = s.Id == _connectedServerId;
                if (s.IsConnected) ServerName = s.Name;
            }
        }

        private async Task RefreshSharesAsync()
        {
            var r = await _client.GetShareAsync();
            if (r?.Shares?.Share != null)
            {
                Shares.Clear();
                foreach (var s in r.Shares.Share) Shares.Add(s);
            }
        }

        private async Task RefreshSearchAsync()
        {
            var r = await _client.GetModifiedAsync("search");
            if (r?.Search != null)
            {
                var cur = r.Search.FirstOrDefault(s => s.id == _currentSearchId);
                if (cur != null) { FoundFiles = cur.FoundFiles; SumSearches = cur.SumSearches; SearchRunning = cur.Running; }
            }
            if (r?.SearchEntry != null)
                foreach (var e in r.SearchEntry)
                    if (e.SearchId == _currentSearchId && !SearchResults.Any(x => x.Id == e.Id))
                        SearchResults.Add(e);
        }

        // ---- Commands impl ----
        private async Task TransferAjLinkAsync()
        {
            await _client.ProcessLinkAsync(Uri.EscapeDataString(AjLinkText.Trim()));
            AjLinkText = string.Empty;
        }

        private async Task StartSearchAsync()
        {
            SearchResults.Clear();
            FoundFiles = 0; SumSearches = 0; SearchRunning = true;
            await _client.StartSearchAsync(SearchText);
            // Aktuelle Such-Id ermitteln (laufende Suche).
            var r = await _client.GetModifiedAsync("search");
            var running = r?.Search?.FirstOrDefault(s => s.Running);
            _currentSearchId = running?.id ?? 0;
        }

        private async Task StopSearchAsync()
        {
            if (_currentSearchId >= 0) await _client.StopSearchAsync(_currentSearchId);
            SearchRunning = false;
        }

        private async Task WithDownload(Func<int, Task> action)
        {
            if (SelectedDownload != null) await action(SelectedDownload.Id);
        }

        private void RaiseDownloadCmds()
        {
            ContinueCommand.RaiseCanExecuteChanged(); BreakCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged(); ApplyPowerDownloadCommand.RaiseCanExecuteChanged();
            SetDownloadPriorityCommand.RaiseCanExecuteChanged(); ReleaseInfoDownloadCommand.RaiseCanExecuteChanged();
            CopyDownloadLinkCommand.RaiseCanExecuteChanged();
        }

        private Task SaveSettings()
        {
            try
            {
                _config.RefreshRate = RefreshRate > 0 ? RefreshRate : 1500;
                _config.ReleaseInfoHost = string.IsNullOrWhiteSpace(ReleaseInfoHost) ? ReleaseInfo.DefaultHost : ReleaseInfoHost.Trim();
                ConfigSerializer.SerializeToFile(_config);
                _timer.Interval = TimeSpan.FromMilliseconds(_config.RefreshRate);
                SettingsStatus = "Gespeichert.";
            }
            catch (Exception ex) { SettingsStatus = "Fehler: " + ex.Message; }
            return Task.CompletedTask;
        }

        private async Task LoadCoreSettingsAsync()
        {
            var s = await _client.GetSettingsAsync();
            if (s == null) { CoreSettingsStatus = "Core-Einstellungen nicht erreichbar."; return; }
            CoreNick = s.Nick ?? string.Empty;
            CorePort = s.Port; CoreXmlPort = s.XmlPort;
            CoreMaxConnections = s.MaxConnections; CoreMaxUpload = s.MaxUpload; CoreMaxDownload = s.MaxDownload;
            CoreSpeedPerSlot = s.SpeedPerSlot; CoreMaxNewConnectionsPerTurn = s.MaxNewConnectionsPerTurn;
            CoreMaxSourcesPerFile = s.MaxSourcesPerFile;
            CoreIncomingDir = s.IncomingDirectory ?? string.Empty; CoreTempDir = s.TemporaryDirectory ?? string.Empty;
            CoreAutoConnect = s.AutoConnect;
            CoreSettingsStatus = "Geladen.";
        }

        private async Task SaveCoreSettingsAsync()
        {
            var s = new ApfelmusFramework.Classes.Settings.Settings
            {
                Nick = CoreNick, Port = CorePort, XmlPort = CoreXmlPort,
                MaxConnections = CoreMaxConnections, MaxUpload = CoreMaxUpload, MaxDownload = CoreMaxDownload,
                SpeedPerSlot = CoreSpeedPerSlot, MaxNewConnectionsPerTurn = CoreMaxNewConnectionsPerTurn,
                MaxSourcesPerFile = CoreMaxSourcesPerFile, IncomingDirectory = CoreIncomingDir,
                TemporaryDirectory = CoreTempDir, AutoConnect = CoreAutoConnect,
            };
            await _client.SetSettingsAsync(s);
            CoreSettingsStatus = "An Core gesendet.";
        }

        private async Task ChangePasswordAsync()
        {
            string newMd5 = await _client.SetPasswordAsync(NewPassword);
            try
            {
                _config.Password = newMd5;
                ConfigSerializer.SerializeToFile(_config);
                CoreSettingsStatus = "Passwort geändert.";
            }
            catch (Exception ex) { CoreSettingsStatus = "Passwort-Fehler: " + ex.Message; }
            NewPassword = string.Empty;
        }

        private Task ApplyLanguage(string code)
        {
            LanguageManager.Apply(code);
            try { _config.LanguageFile = code; ConfigSerializer.SerializeToFile(_config); } catch { }
            return Task.CompletedTask;
        }

        private Task ApplyTheme(string theme)
        {
            if (Application.Current != null)
                Application.Current.RequestedThemeVariant =
                    string.Equals(theme, ThemeNames.Light, StringComparison.OrdinalIgnoreCase) ? ThemeVariant.Light : ThemeVariant.Dark;
            try { _config.Theme = theme; ConfigSerializer.SerializeToFile(_config); } catch { }
            return Task.CompletedTask;
        }

        private void OpenReleaseInfo(string? name, string? hash, string? size)
        {
            string host = string.IsNullOrWhiteSpace(_config.ReleaseInfoHost) ? ReleaseInfo.DefaultHost : _config.ReleaseInfoHost;
            ReleaseInfo.Open(host, name, hash, size);
        }

        private static string BuildAjfsp(string? name, string? hash, string? size)
            => "ajfsp://file|" + (name ?? string.Empty) + "|" + hash + "|" + size + "/";

        private void RaiseCopy(string text) => CopyRequested?.Invoke(text);

        private static void MergeById<T, TKey>(ObservableCollection<T> target, IEnumerable<T> source, Func<T, TKey> key, Action<T, T> update)
            where TKey : notnull
        {
            var incoming = source.ToList();
            var keys = new HashSet<TKey>(incoming.Select(key));
            for (int i = target.Count - 1; i >= 0; i--)
                if (!keys.Contains(key(target[i]))) target.RemoveAt(i);
            var existing = target.ToDictionary(key);
            foreach (var item in incoming)
            {
                if (existing.TryGetValue(key(item), out var cur)) update(cur, item);
                else target.Add(item);
            }
        }
    }
}
