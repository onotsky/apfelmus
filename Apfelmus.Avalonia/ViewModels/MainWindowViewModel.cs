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

        public MainWindowViewModel(Config config, string? startupLink = null)
        {
            _config = config;
            _client = new CoreClient(config);
            _registerProtocol = config.ProtocolHandler;

            Downloads = new ObservableCollection<Download>();
            DownloadSources = new ObservableCollection<User>();
            ActiveUploads = new ObservableCollection<Upload>();
            QueuedUploads = new ObservableCollection<Upload>();
            Servers = new ObservableCollection<Server>();
            SearchTabs = new ObservableCollection<SearchTabViewModel>();
            Shares = new ObservableCollection<ShareItem>();
            FilteredShares = new ObservableCollection<ShareItem>();
            ShareTree = new ObservableCollection<DirNodeViewModel>();

            PowerValues = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5 };
            PriorityValues = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
            _selectedPowerValue = 0;
            _selectedPriority = 1;

            _guiVersion = Assembly.GetExecutingAssembly().GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "-";
            _refreshRate = config.RefreshRate > 0 ? config.RefreshRate : 1500;
            _releaseInfoHost = string.IsNullOrWhiteSpace(config.ReleaseInfoHost) ? ReleaseInfo.DefaultHost : config.ReleaseInfoHost;
            _showLoginNextStart = !config.HideLoginWindow;

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
            CopyDownloadSourceCommand = new RelayCommand(() => { if (SelectedDownload != null) RaiseCopy(BuildSourceLink(SelectedDownload.FileName, SelectedDownload.Hash, SelectedDownload.Size)); return Task.CompletedTask; }, () => SelectedDownload != null);
            RenameCommand = new RelayCommand(() => { if (SelectedDownload != null) RenameRequested?.Invoke(SelectedDownload); return Task.CompletedTask; }, () => SelectedDownload != null);

            // Server
            ConnectServerCommand = new RelayCommand(async () => { if (SelectedServer != null) await _client.ConnectServerAsync(SelectedServer.Id); }, () => SelectedServer != null);
            RemoveServerCommand = new RelayCommand(async () => { if (SelectedServer != null) { await _client.RemoveServerAsync(SelectedServer.Id); await RefreshServersAsync(); } }, () => SelectedServer != null);
            AddOfficialServersCommand = new RelayCommand(() => _client.AddOfficialServersAsync());

            // Suche
            StartSearchCommand = new RelayCommand(StartSearchAsync, () => !string.IsNullOrWhiteSpace(SearchText));
            StopSearchCommand = new RelayCommand(StopSearchAsync, () => SelectedSearchTab is { Running: true });
            CloseSearchTabCommand = new RelayCommand(CloseSearchTabAsync, () => SelectedSearchTab != null);
            DownloadSearchResultCommand = new RelayCommand(async () => { var r = SelectedSearchTab?.SelectedResult; if (r != null) await _client.StartDownloadAsync(r.FileName?.Name ?? string.Empty, r.Checksum, r.Size.ToString()); });
            ReleaseInfoSearchCommand = new RelayCommand(() => { var r = SelectedSearchTab?.SelectedResult; if (r != null) OpenReleaseInfo(r.FileName?.Name, r.Checksum, r.Size.ToString()); return Task.CompletedTask; });
            CopySearchLinkCommand = new RelayCommand(() => { var r = SelectedSearchTab?.SelectedResult; if (r != null) RaiseCopy(BuildAjfsp(r.FileName?.Name, r.Checksum, r.Size.ToString())); return Task.CompletedTask; });

            // Share
            SetSharePriorityCommand = new RelayCommand(async () => { if (SelectedShare != null) { await _client.SetDownloadPriorityAsync(SelectedShare.Id, SelectedPriority); } }, () => SelectedShare != null);
            DelSharePriorityCommand = new RelayCommand(async () => { if (SelectedShare != null) { await _client.SetDownloadPriorityAsync(SelectedShare.Id, 1); } }, () => SelectedShare != null);
            ReleaseInfoShareCommand = new RelayCommand(() => { if (SelectedShare != null) OpenReleaseInfo(SelectedShare.ShortFileName, SelectedShare.CheckSum, SelectedShare.Size.ToString()); return Task.CompletedTask; }, () => SelectedShare != null);
            CopyShareLinkCommand = new RelayCommand(() => { if (SelectedShare != null) RaiseCopy(BuildAjfsp(SelectedShare.ShortFileName, SelectedShare.CheckSum, SelectedShare.Size.ToString())); return Task.CompletedTask; }, () => SelectedShare != null);

            // Freigabe-Verzeichnisbaum
            LoadShareTreeCommand = new RelayCommand(LoadShareTreeAsync);
            ShareFolderCommand = new RelayCommand(() => ShareSelectedAsync(true), () => SelectedShareNode != null);
            ShareFolderNoSubCommand = new RelayCommand(() => ShareSelectedAsync(false), () => SelectedShareNode != null);
            UnshareFolderCommand = new RelayCommand(UnshareSelectedAsync, () => SelectedShareNode != null);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_refreshRate) };
            _timer.Tick += async (_, _) => await PollAsync();
            _timer.Start();
            _ = PollAsync();
            _ = LoadCoreSettingsAsync();

            // Gespeicherte Sprache anwenden.
            LanguageManager.Apply(LanguageManager.Normalize(config.LanguageFile));

            // Beim Start uebergebener ajfsp-Link (Protokoll-Handler) -> an den Core weiterreichen.
            if (!string.IsNullOrWhiteSpace(startupLink))
                _ = _client.ProcessLinkAsync(Uri.EscapeDataString(startupLink!.Trim()));
        }

        /// <summary>Wird von der View gesetzt/abonniert, um Text in die Zwischenablage zu legen.</summary>
        public event Action<string>? CopyRequested;

        // ---- Collections ----
        public ObservableCollection<Download> Downloads { get; }
        public ObservableCollection<User> DownloadSources { get; }
        public ObservableCollection<Upload> ActiveUploads { get; }
        public ObservableCollection<Upload> QueuedUploads { get; }
        public ObservableCollection<Server> Servers { get; }
        public ObservableCollection<SearchTabViewModel> SearchTabs { get; }
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
        public RelayCommand CopyDownloadSourceCommand { get; }
        public RelayCommand RenameCommand { get; }

        /// <summary>Wird von der View abonniert, um den Umbenennen-Dialog anzuzeigen.</summary>
        public event Action<Download>? RenameRequested;
        public RelayCommand ConnectServerCommand { get; }
        public RelayCommand RemoveServerCommand { get; }
        public RelayCommand AddOfficialServersCommand { get; }
        public RelayCommand StartSearchCommand { get; }
        public RelayCommand StopSearchCommand { get; }
        public RelayCommand CloseSearchTabCommand { get; }
        public RelayCommand DownloadSearchResultCommand { get; }
        public RelayCommand ReleaseInfoSearchCommand { get; }
        public RelayCommand CopySearchLinkCommand { get; }
        public RelayCommand SetSharePriorityCommand { get; }
        public RelayCommand DelSharePriorityCommand { get; }
        public RelayCommand ReleaseInfoShareCommand { get; }
        public RelayCommand CopyShareLinkCommand { get; }
        public RelayCommand LoadShareTreeCommand { get; }
        public RelayCommand ShareFolderCommand { get; }
        public RelayCommand ShareFolderNoSubCommand { get; }
        public RelayCommand UnshareFolderCommand { get; }

        public ObservableCollection<DirNodeViewModel> ShareTree { get; }

        /// <summary>Freigegebene Dateien, gefiltert auf den im Baum gewaehlten Ordner (explorer-artig, wie WPF).</summary>
        public ObservableCollection<ShareItem> FilteredShares { get; }

        private DirNodeViewModel? _selectedShareNode;
        public DirNodeViewModel? SelectedShareNode
        {
            get => _selectedShareNode;
            set
            {
                if (SetProperty(ref _selectedShareNode, value))
                {
                    ShareFolderCommand.RaiseCanExecuteChanged();
                    ShareFolderNoSubCommand.RaiseCanExecuteChanged();
                    UnshareFolderCommand.RaiseCanExecuteChanged();
                    ApplyShareFilter();
                }
            }
        }

        private void ApplyShareFilter()
        {
            string? path = SelectedShareNode?.Path;
            FilteredShares.Clear();
            foreach (var s in Shares)
            {
                if (string.IsNullOrEmpty(path)
                    || (s.FileName != null && s.FileName.StartsWith(path, System.StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredShares.Add(s);
                }
            }
        }

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
        private SearchTabViewModel? _selectedSearchTab;
        private ShareItem? _selectedShare;
        private string _searchText = string.Empty;
        private string _ajLinkText = string.Empty;
        private int _selectedPowerValue, _selectedPriority;
        private int _refreshRate;
        private string _releaseInfoHost, _settingsStatus = string.Empty;
        private bool _showLoginNextStart;
        private bool _registerProtocol;

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
        public SearchTabViewModel? SelectedSearchTab
        {
            get => _selectedSearchTab;
            set { if (SetProperty(ref _selectedSearchTab, value)) { StopSearchCommand.RaiseCanExecuteChanged(); CloseSearchTabCommand.RaiseCanExecuteChanged(); } }
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
        public int RefreshRate { get => _refreshRate; set => SetProperty(ref _refreshRate, value); }
        public string ReleaseInfoHost { get => _releaseInfoHost; set => SetProperty(ref _releaseInfoHost, value); }
        public bool ShowLoginNextStart { get => _showLoginNextStart; set => SetProperty(ref _showLoginNextStart, value); }
        public bool RegisterAjfspProtocol { get => _registerProtocol; set => SetProperty(ref _registerProtocol, value); }
        /// <summary>Nur unter Windows sinnvoll (Registry) - steuert die Sichtbarkeit der Checkbox.</summary>
        public bool IsProtocolSupported => Services.ProtocolHandlerService.IsSupported;
        public string SettingsStatus { get => _settingsStatus; private set => SetProperty(ref _settingsStatus, value); }

        // ---- Core-Einstellungen (settings.xml) ----
        private string _coreNick = string.Empty, _coreIncomingDir = string.Empty, _coreTempDir = string.Empty;
        private string _newPassword = string.Empty, _coreSettingsStatus = string.Empty;
        private int _corePort, _coreXmlPort, _coreMaxConnections, _coreMaxUpload, _coreMaxDownload, _coreSpeedPerSlot, _coreMaxNewConn, _coreMaxSources;
        private bool _coreAutoConnect;
        private ApfelmusFramework.Classes.Settings.Settings? _coreSettings;

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

            await RefreshUsersAsync();
            await RefreshDownloadsAsync();
            await RefreshSharesAsync();
            await RefreshUploadsAsync();
            await RefreshServersAsync();
            await RefreshSearchAsync();
        }

        private int _connectedServerId;

        private async Task RefreshDownloadsAsync()
        {
            var r = await _client.GetModifiedAsync("down");
            if (r?.Download == null) return;

            // Der Core liefert aktive Quellen/Speed/Fortschritt NICHT im Download-XML - sie werden
            // (wie im WPF-Client) aus der User-Liste berechnet: aktive Quelle = Status 7.
            foreach (var d in r.Download) ComputeDownloadDerived(d);

            MergeById(Downloads, r.Download, x => x.Id, (t, s) =>
            {
                t.FileName = s.FileName; t.Size = s.Size; t.Status = s.Status; t.Speed = s.Speed;
                t.ActiveUsers = s.ActiveUsers; t.AllUsers = s.AllUsers; t.DownloadUsers = s.DownloadUsers;
                t.Percentages = s.Percentages; t.DownloadedFilesize = s.DownloadedFilesize;
                t.DownloadRest = s.DownloadRest; t.CheckIfIsOver = s.CheckIfIsOver;
                t.TimeToEnd = s.TimeToEnd; t.PowerDownload = s.PowerDownload;
            });
        }

        private void ComputeDownloadDerived(Download d)
        {
            var us = _allUsers.Where(u => u.DownloadId == d.Id).ToList();
            d.ActiveUsers = us.Count(u => u.Status == 7);
            d.DownloadUsers = us.Count(u => u.Status == 5 || u.Status == 7);
            d.AllUsers = us.Count;
            d.Speed = us.Where(u => u.Status == 7).Sum(u => u.Speed);

            long size = ParseLong(d.Size);
            long ready = ParseLong(d.Ready);
            // d.Status-Getter hebt 0->2, wenn aktive Quellen vorhanden sind (siehe DTO).
            if (d.Status == 14)
            {
                d.DownloadedFilesize = d.Size; d.CheckIfIsOver = d.Size; d.DownloadRest = "0"; d.Percentages = "100 %";
            }
            else
            {
                d.DownloadedFilesize = d.Ready; d.CheckIfIsOver = d.Ready;
                d.DownloadRest = (size - ready).ToString();
                d.Percentages = size > 0 ? Math.Round(ready / (double)size * 100.0, 2) + " %" : "0 %";
            }
        }

        private static long ParseLong(string? s) => long.TryParse(s, out var v) ? v : 0;

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
            if (r?.Upload == null) return;

            ActiveUploads.Clear();
            QueuedUploads.Clear();
            foreach (var u in r.Upload)
            {
                // Dateiname stammt nicht vom Core im Upload selbst, sondern aus der Freigabe (Shareid).
                var share = Shares.FirstOrDefault(s => s.Id == u.Shareid);
                if (share != null) u.FileName = share.ShortFileName;
                if (u.UploadTo > u.UploadFrom)
                    u.Percentages = Math.Round((double)(u.ActualUploadPosition - u.UploadFrom) / (u.UploadTo - u.UploadFrom) * 100.0, 2) + " %";

                if (u.Status == 1) ActiveUploads.Add(u);
                else QueuedUploads.Add(u);
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
                ApplyShareFilter();
            }
        }

        private async Task RefreshSearchAsync()
        {
            if (SearchTabs.Count == 0) return;
            var r = await _client.GetModifiedAsync("search");
            if (r == null) return;

            foreach (var tab in SearchTabs)
            {
                var cur = r.Search?.FirstOrDefault(s => s.id == tab.Id);
                if (cur != null) { tab.FoundFiles = cur.FoundFiles; tab.SumSearches = cur.SumSearches; tab.Running = cur.Running; }
                if (r.SearchEntry != null)
                    foreach (var e in r.SearchEntry)
                        if (e.SearchId == tab.Id && !tab.Results.Any(x => x.Id == e.Id))
                            tab.Results.Add(e);
            }
            StopSearchCommand.RaiseCanExecuteChanged();
        }

        // ---- Commands impl ----
        private async Task TransferAjLinkAsync()
        {
            await _client.ProcessLinkAsync(Uri.EscapeDataString(AjLinkText.Trim()));
            AjLinkText = string.Empty;
        }

        private async Task StartSearchAsync()
        {
            var tab = new SearchTabViewModel(SearchText) { Running = true };
            SearchTabs.Add(tab);
            SelectedSearchTab = tab;

            await _client.StartSearchAsync(SearchText);

            // Neue Such-Id ermitteln: die neueste laufende Suche, die noch keinem Tab zugeordnet ist.
            var known = SearchTabs.Where(t => t.Id != 0).Select(t => t.Id).ToHashSet();
            var r = await _client.GetModifiedAsync("search");
            var running = r?.Search?
                .Where(s => !known.Contains(s.id))
                .OrderByDescending(s => s.id)
                .FirstOrDefault();
            if (running != null) tab.Id = running.id;

            SearchText = string.Empty;
        }

        private async Task StopSearchAsync()
        {
            var tab = SelectedSearchTab;
            if (tab == null) return;
            if (tab.Id != 0) await _client.StopSearchAsync(tab.Id);
            tab.Running = false;
            StopSearchCommand.RaiseCanExecuteChanged();
        }

        private async Task CloseSearchTabAsync()
        {
            var tab = SelectedSearchTab;
            if (tab == null) return;
            if (tab.Id != 0 && tab.Running) await _client.StopSearchAsync(tab.Id);
            SearchTabs.Remove(tab);
            SelectedSearchTab = SearchTabs.LastOrDefault();
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
            CopyDownloadLinkCommand.RaiseCanExecuteChanged(); CopyDownloadSourceCommand.RaiseCanExecuteChanged();
            RenameCommand.RaiseCanExecuteChanged();
        }

        /// <summary>Fuehrt das Umbenennen aus (vom Dialog der View aufgerufen).</summary>
        public void ExecuteRename(int downloadId, string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
                _ = _client.RenameDownloadAsync(downloadId, newName.Trim());
        }

        private string BuildSourceLink(string? name, string? hash, string? size)
        {
            var srv = Servers.FirstOrDefault(s => s.IsConnected);
            int corePort = _coreSettings?.Port ?? 0;
            if (srv == null || string.IsNullOrEmpty(OwnIp) || OwnIp == "-")
                return BuildAjfsp(name, hash, size);
            return $"ajfsp://file|{name}|{hash}|{size}|{OwnIp}:{corePort}:{srv.Host}:{srv.Port}/";
        }

        private Task SaveSettings()
        {
            try
            {
                _config.RefreshRate = RefreshRate > 0 ? RefreshRate : 1500;
                _config.ReleaseInfoHost = string.IsNullOrWhiteSpace(ReleaseInfoHost) ? ReleaseInfo.DefaultHost : ReleaseInfoHost.Trim();
                _config.HideLoginWindow = !ShowLoginNextStart;
                _config.ProtocolHandler = RegisterAjfspProtocol;
                ConfigSerializer.SerializeToFile(_config);
                _timer.Interval = TimeSpan.FromMilliseconds(_config.RefreshRate);

                if (RegisterAjfspProtocol && Services.ProtocolHandlerService.IsSupported)
                {
                    bool ok = Services.ProtocolHandlerService.Register();
                    SettingsStatus = ok ? "Gespeichert. ajfsp-Verknüpfung registriert." : "Gespeichert (ajfsp-Registrierung fehlgeschlagen).";
                }
                else
                {
                    SettingsStatus = "Gespeichert.";
                }
            }
            catch (Exception ex) { SettingsStatus = "Fehler: " + ex.Message; }
            return Task.CompletedTask;
        }

        private async Task LoadCoreSettingsAsync()
        {
            var s = await _client.GetSettingsAsync();
            if (s == null) { CoreSettingsStatus = "Core-Einstellungen nicht erreichbar."; return; }
            _coreSettings = s;
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

        private async Task LoadShareTreeAsync()
        {
            var aj = await _client.GetDirectoryAsync(null);
            ShareTree.Clear();
            if (aj?.Dir == null) return;
            string sep = aj.FileSystem?.Seperator ?? "/";
            foreach (var d in aj.Dir.OrderBy(x => x.Name))
            {
                if (string.IsNullOrEmpty(d.Path))
                    d.Path = sep == "/" ? $"{d.Name}{sep}" : $"{d.Name}{sep}";
                ShareTree.Add(new DirNodeViewModel(d, _client));
            }
        }

        /// <summary>Aktuelle Freigabeliste aus den geladenen Core-Einstellungen (Pfad + Unterordner-Flag).</summary>
        private List<(string path, bool sub)> CurrentShares()
        {
            var list = new List<(string, bool)>();
            var dirs = _coreSettings?.share?.Directory;
            if (dirs != null)
                foreach (var d in dirs)
                    list.Add((d.Name ?? string.Empty, ParseShareMode(d.ShareMode)));
            return list;
        }

        private static bool ParseShareMode(string? mode)
            => !string.IsNullOrEmpty(mode) && (mode.Equals("true", StringComparison.OrdinalIgnoreCase) || mode == "1");

        private async Task ShareSelectedAsync(bool includeSubdirs)
        {
            var node = SelectedShareNode;
            if (node == null) return;
            var list = CurrentShares();
            int idx = list.FindIndex(x => x.path == node.Path);
            if (idx >= 0) list[idx] = (node.Path, includeSubdirs);
            else list.Add((node.Path, includeSubdirs));
            await _client.SetSharesAsync(list);
            await LoadCoreSettingsAsync();
        }

        private async Task UnshareSelectedAsync()
        {
            var node = SelectedShareNode;
            if (node == null) return;
            var list = CurrentShares().Where(x => x.path != node.Path).ToList();
            await _client.SetSharesAsync(list);
            await LoadCoreSettingsAsync();
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
