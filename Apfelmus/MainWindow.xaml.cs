//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace Apfelmus
{
    using ApfelmusFramework.Classes.Allgemein;
    using Config = ApfelmusFramework.Classes.Config.Config;
    using ApfelmusFramework.Classes.Directory;
    using ApfelmusFramework.Classes.ExtensionSort;
    using ApfelmusFramework.Classes.Help;
    using ApfelmusFramework.Classes.Logic;
    using ApfelmusFramework.Classes.Modified;
    using ApfelmusFramework.Classes.Serializer;
    using ApfelmusFramework.Classes.Settings;
    using ApfelmusFramework.Classes.Share;
    using log4net;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shell;
    using System.Windows.Threading;


    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields
        private AppleJuice appleJuice = new AppleJuice();
        private Settings settings = new Settings();

        private Information info = new Information();
        private NetworkInfo nInfo = new NetworkInfo();
        private ResourceDictionary dict;
        private SearchInfo searchInfo = new SearchInfo();
        private CloseableTab.CloseableTabItem cTabItem;

        private Thread _updateUploads;
        private Thread _updateInformation;
        private Thread _updateDownloads;
        private volatile Thread _updateUsers;
        private Thread _listenArguments;
        private Thread _refreshServer;
        private Thread _checkConnectionToCore;
        private Thread _searchThread;
        // Kooperatives Stopp-Signal fuer den Such-Thread. Thread.Abort() gibt es unter .NET (Core)
        // nicht mehr (wirft PlatformNotSupportedException) - der Thread muss selbst regelmaessig
        // pruefen, ob er beendet werden soll.
        private volatile bool _stopSearch;
        private volatile Thread _refreshDownloadPartList;
        private volatile Thread _refreshUserPartList;
        private DataGrid selectionDownloadGrid = new DataGrid();
        private Download selectedDownload = new Download();
        private DataGrid selectionUserGrid = new DataGrid();
        private User selectedUser = new User();

        private ObservableCollection<Upload> currentUploads = new ObservableCollection<Upload>();
        private ObservableCollection<Upload> waitingUploads = new ObservableCollection<Upload>();
        private ObservableCollection<Download> downloads = new ObservableCollection<Download>();
        private ObservableCollection<User> users = new ObservableCollection<User>();
        private ObservableCollection<Server> server = new ObservableCollection<Server>();
        private ObservableCollection<Share> _shares = new ObservableCollection<Share>();
        private ObservableCollection<SettingsDirectory> sDir = new ObservableCollection<SettingsDirectory>();
        private ObservableCollection<Part> parts = new ObservableCollection<Part>();

        private ListCollectionView lcvShares;
        private SortDescription sdShares = new SortDescription("ShortFileName", ListSortDirection.Ascending);

        public static Config config;
        private ILog logger;

        private string getModifiedInfos;
        private string getModifiedDownloads;
        private string getModifiedUploads;
        private string getModifiedIds;
        private string getModifiedServer;
        private string getModifiedUser;
        private string getInformation;
        private string getSearch;
        private string getShare;
        private string getSettings;

        public static ConcurrentBag<string> args = new ConcurrentBag<string>();
        private ObservableCollection<KeyValuePair<int, double>> _powerValues = new ObservableCollection<KeyValuePair<int, double>>();
        private List<KeyValuePair<int, int>> priority = new List<KeyValuePair<int, int>>();
        private List<Server> _server = new List<Server>();
        private List<DirectoryChildren> item = new List<DirectoryChildren>();
        #endregion

        #region Properties
        public ObservableCollection<KeyValuePair<int, double>> PowerValues
        {
            get => _powerValues;
            set => _powerValues = value;
        }

        public DirectoryChildren Item { get; set; }

        public List<KeyValuePair<int, int>> Priority
        {
            get => priority;
            set => priority = value;
        }

        #endregion

        #region Delegates
        private delegate void UpdateUploadDataGrid();
        private delegate void ActivateButtons();
        private delegate void UpdateSearchCollection(ObservableCollection<SearchEntry> searchEntry, int id);
        private delegate void UpdateAppleJuiceObj();
        private delegate void UpdateInformations();
        private delegate void UpdateDownloadDataGrid();
        private delegate void UpdateUserDataGrid();
        private delegate void SetTabDownload();
        private delegate void UpdateServerlist();
        private delegate void RefreshDownloadPartlist();
        private delegate void RefreshUserPartList();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the BaseWindow class
        /// </summary>
        public MainWindow(string[] args)
        {
            InitializeComponent();

            log4net.Config.XmlConfigurator.Configure();
            logger = LogManager.GetLogger(typeof(MainWindow));

            AddHandler(CloseableTab.CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));

            if (args.Length > 0)
            {
                MainWindow.args.Add(args[0]);
            }

            PowerValues.Add(new KeyValuePair<int, double>(0, 1.0));
            for (int i = 12; i < 491; i++)
            {
                PowerValues.Add(new KeyValuePair<int, double>(i, Math.Round(((i + 10.0) / 10.0), 1)));
            }

            for (int i = 1; i < 251; i++)
            {
                Priority.Add(new KeyValuePair<int, int>(i, i));
            }

            BaseWindow.CheckConfig();

            try
            {
                config = ConfigSerializer.DeserializeFromFile();
                if (config.LanguageFile != null)
                {
                    Resources.MergedDictionaries.Add(LanguageDictionary.GetLanguageDictionary(config.LanguageFile));
                }
                else
                {
                    Resources.MergedDictionaries.Add(LanguageDictionary.GetLanguageDictionary());
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Lesen der Config", ex);
            }

            if (!BaseWindow.config.HideLoginWindow)
            {
                BaseWindow baseWindow = new BaseWindow(this, Resources)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                baseWindow.ShowDialog();
            }

            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                if (!webConnect.CheckSocket(config.HostName, config.Port))
                {
                    BaseWindow baseWindow = new BaseWindow(this, Resources)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    baseWindow.ShowDialog();
                }
            }

            SetXmlCalls();
            CreateSubObjects();
            StartThreads();
            SetBindings();

            OwnSplashScreen splashScreen = new OwnSplashScreen
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            splashScreen.ShowDialog();
        }

        private void _activateButtons()
        {
            btnStartSearch.IsEnabled = true;
            pBarSearchCount.IsIndeterminate = false;
        }
        #endregion

        #region ThreadMethods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _updateUploads_ThreadWorker()
        {
            UpdateUploadDataGrid updateUploadDataGrid = new UpdateUploadDataGrid(RefreshUploadDatagrid);
            while ((Thread.CurrentThread.ThreadState & ThreadState.Running) == ThreadState.Running)
            {
                try
                {
                    Dispatcher.Invoke(updateUploadDataGrid, null);
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Holen der aktuellen Uploads!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        /// <summary>
        /// DoWorkevent zur AppleJuice Suche
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _search_ThreadWorker(object obj)
        {
            ActivateButtons aButtons = new ActivateButtons(_activateButtons);

            // FirstOrDefault statt First: findet sich keine laufende Suche (Race beim Start),
            // wird der Thread sauber beendet statt mit einer Exception zu sterben.
            Search _getSearch = appleJuice.Search.AsEnumerable().FirstOrDefault(a => a.Running.Equals(true));
            if (_getSearch == null)
            {
                Dispatcher.Invoke(aButtons, null);
                return;
            }

            int id = _getSearch.id;
            object[] param = new object[] { (ObservableCollection<SearchEntry>)obj, id };
            UpdateSearchCollection uSearch = new UpdateSearchCollection(_startSearch);

            while (!_stopSearch)
            {
                try
                {
                    Search current = appleJuice.Search.AsEnumerable().FirstOrDefault(a => a.id.Equals(id));
                    if (current == null || !current.Running)
                    {
                        break;
                    }

                    Dispatcher.Invoke(uSearch, param);
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Ausführen der Suche!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }

            Dispatcher.Invoke(aButtons, null);
        }

        /// <summary>
        /// ThreadMethode zum Update der Informationen 
        /// </summary>
        private void _updateInformation_ThreadWorker()
        {
            while ((Thread.CurrentThread.ThreadState & ThreadState.Running) == ThreadState.Running)
            {
                try
                {
                    UpdateInformation();
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Update der Informationen", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        /// <summary>
        /// ThreadMethode zum Update der Downloads 
        /// </summary>
        private void _updateDownloads_ThreadWorker()
        {
            UpdateDownloadDataGrid updateDownloadDataGrid = new UpdateDownloadDataGrid(RefreshDownloadDatagrid);

            while ((Thread.CurrentThread.ThreadState & ThreadState.Running) == ThreadState.Running)
            {
                try
                {
                    Dispatcher.Invoke(updateDownloadDataGrid, null);
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Erstellen der Downloads!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        /// <summary>
        /// ThreadMethode zum Update der User 
        /// </summary>
        private void _updateUsers_ThreadStart()
        {
            UpdateUserDataGrid updateUserDataGrid = new UpdateUserDataGrid(RefreshUserDatagrid);

            // Laeuft, solange dieser Thread der "aktuelle" ist. Wird das Feld auf null gesetzt
            // oder durch einen neuen Thread ersetzt, beendet sich der alte selbst - kooperativ,
            // da es Thread.Abort() unter .NET (Core) nicht mehr gibt (PlatformNotSupportedException).
            while (Thread.CurrentThread == _updateUsers)
            {
                try
                {
                    Dispatcher.Invoke(updateUserDataGrid, null);
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Erstellen der Userliste!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        /// <summary>
        /// ThreadMethode zum Verarbeiten der übergebenen Argumente
        /// </summary>
        private void _listenArguments_ThreadWorker()
        {
            SetTabDownload setTabDownload = new SetTabDownload(SetTabDownloadTrue);
            while ((Thread.CurrentThread.ThreadState & ThreadState.Running) == ThreadState.Running)
            {
                try
                {
                    while (!args.IsEmpty)
                    {
                        ProcessArgs();
                        Dispatcher.Invoke(setTabDownload, null);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Verarbeiten der übergebenen Argumente!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        /// <summary>
        /// ThreadMethode zum Update der Server 
        /// </summary>
        private void _refreshServer_ThreadWorker()
        {
            UpdateServerlist updateServerlist = new UpdateServerlist(RefreshServerList);
            while ((Thread.CurrentThread.ThreadState & ThreadState.Running) == ThreadState.Running)
            {
                try
                {
                    Dispatcher.Invoke(updateServerlist, null);
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Erstellen der Serverliste!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        /// <summary>
        /// ThreadMethode zum Überprüfen der Verbindung zur Core
        /// </summary>
        private void _checkConnectionToCore_ThreadWorker()
        {
            while ((Thread.CurrentThread.ThreadState & ThreadState.Running) == ThreadState.Running)
            {
                try
                {
                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        if (!webConnect.CheckSocket(config.HostName, config.Port))
                        {
                            MessageBox.Show("Verbindung zur Core unterbrochen", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(10);
                        }


                        string meldung = webConnect.GetHttpResult(getModifiedInfos, config.UseCompression);
                        if (meldung != null && meldung.Contains("Passwort falsch"))
                        {
                            MessageBox.Show("Passwort ist falsch!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim Überprüfen der Coreverbindung!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        private void _refreshDownloadPartList_ThreadWorker()
        {
            RefreshDownloadPartlist refreshDownloadPartlist = new RefreshDownloadPartlist(CreateDownloadPartlist);

            // Kooperativ beenden: laeuft nur, solange dieser Thread der aktuelle ist (Feld auf null
            // setzen / neu zuweisen stoppt den alten). Thread.Abort() gibt es unter .NET nicht mehr.
            while (Thread.CurrentThread == _refreshDownloadPartList)
            {
                try
                {
                    Dispatcher.Invoke(refreshDownloadPartlist, null);
                }
                catch (Exception ex)
                {
                    logger.Warn("Thread (RefreshDownloadPartList) wurde beendet!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }

        private void _refreshUserPartList_ThreadWorker()
        {
            RefreshUserPartList refreshUserPartlist = new RefreshUserPartList(CreateUserPartlist);

            // Kooperativ beenden: laeuft nur, solange dieser Thread der aktuelle ist (Feld auf null
            // setzen / neu zuweisen stoppt den alten). Thread.Abort() gibt es unter .NET nicht mehr.
            while (Thread.CurrentThread == _refreshUserPartList)
            {
                try
                {
                    if (selectedUser != null)
                    {
                        Dispatcher.Invoke(refreshUserPartlist, null);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Fehler beim erzeugen der Userpartliste!", ex);
                }
                finally
                {
                    Thread.Sleep(config.RefreshRate);
                }
            }
        }
        #endregion

        #region HelpMethods

        /// <summary>
        /// Hilfsmethode zum Wechsel auf das Downloadtab
        /// </summary>
        private void SetTabDownloadTrue()
        {
            if (!tabDownload.IsSelected)
            {
                tabDownload.IsSelected = true;
            }
        }

        /// <summary>
        /// Hilfsmethode zum ausführen des Downloads einer Datei
        /// </summary>
        private void ProcessLink()
        {
            var link = tbxDownload.Text;

            if (!link.Contains('|'))
                link.Replace("%7C", "|");

            string[] tempArgs = link.ToString().Split('|');

            tempArgs[1] = WebUtility.UrlEncode(tempArgs[1]);
            link = string.Format("{0}|{1}|{2}|{3}", tempArgs);

            string processLink = "/function/processlink?link=" + link + "&password=" + config.Password;
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                webConnect.StartXMLFunction(processLink);
            }
        }

        /// <summary>
        /// Hilfsmethode zum ausführen des Downloads für n Dateien
        /// </summary>
        public void ProcessArgs()
        {
            string _item = string.Empty;

            if (args.TryTake(out _item))
            {
                if (!_item.Contains('|'))
                    _item = _item.Replace("%7C", "|");

                string[] tempArgs = _item.ToString().Split('|');

                tempArgs[1] = WebUtility.UrlEncode(tempArgs[1]);
                _item = string.Format("{0}|{1}|{2}|{3}", tempArgs);
            }

            string processLink = "/function/processlink?link=" + _item + "&password=" + config.Password;
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                webConnect.StartXMLFunction(processLink);
            }
        }

        /// <summary>
        /// Hilfsmethode zum setzen der Bindings der UI
        /// </summary>
        public void SetBindings()
        {
            try
            {
                appleJuice.GeneralInformation.Release = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? Assembly.GetExecutingAssembly().GetName().Version.ToString();
                lblCore.DataContext = appleJuice.GeneralInformation;
                lblFiles.DataContext = nInfo;
                lblFileSize.DataContext = nInfo;
                imgFirewall.DataContext = nInfo;
                lblIp.DataContext = nInfo;
                lblOpenConnections.DataContext = info;
                lblUploadQueue.DataContext = info;
                lblUsers.DataContext = nInfo;
                lblGui.DataContext = appleJuice.GeneralInformation;
                lblServerName.DataContext = appleJuice.Server.AsEnumerable().Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).Select(a => a);
                lblConnectedSince.DataContext = nInfo;
                txtBlockServerMessage.DataContext = nInfo;
                appleJuice.Download.Sort(a => a.FileName);
                dGridDownloads.ItemsSource = downloads;
                dGridDownloadInfos.ItemsSource = users;

                dGridUploads.ItemsSource = currentUploads;
                dGridUploadInfos.ItemsSource = waitingUploads;
                dGridServer.ItemsSource = server;
                lcvShares = new ListCollectionView(_shares);
                lcvShares.GroupDescriptions.Add(new PropertyGroupDescription("Path"));
                dGridShares.ItemsSource = lcvShares;
                lblCreditsValue.DataContext = info;
                lblUploadsValue.DataContext = info;
                lblDownloadsValue.DataContext = info;
                lblTrafficInValue.DataContext = info;
                lblTrafficOutValue.DataContext = info;
                pBarSearch.DataContext = searchInfo;
                lblFoundFilesResult.DataContext = searchInfo;
                lblSumSearchesResult.DataContext = searchInfo;
                lblEndSearch.DataContext = searchInfo;

                tViewSharePaths.ItemsSource = item;

                DataContext = this;
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Setzen der Bindings!", ex);
            }
        }

        private void GenerateFirstTreeItem()
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                appleJuice.Dir = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/directory.xml?password={0}&mode=zip", config.Password), config.UseCompression)) as AppleJuice).Dir;
            }

            List<Dir> dirList = new List<Dir>();
            foreach (Dir _dir in appleJuice.Dir)
            {
                if (string.IsNullOrEmpty(_dir.Path))
                {
                    if (appleJuice.GeneralInformation.FileSystem.Seperator.Equals("/"))
                    {
                        _dir.Path = _dir.Name;
                    }
                    else
                    {
                        _dir.Path = string.Format("{0}{1}", appleJuice.GeneralInformation.FileSystem.Seperator, _dir.Name);
                    }
                }
                item.Add(new DirectoryChildren(_dir, config));

                foreach (Dir __dir in _dir.Directory)
                {
                    if (string.IsNullOrEmpty(__dir.Path))
                    {
                        __dir.Path = string.Format("{0}{1}{2}", _dir.Path, appleJuice.GeneralInformation.FileSystem.Seperator, __dir.Name);
                    }

                    item.Add(new DirectoryChildren(__dir, config));
                }
            }
        }

        /// <summary>
        /// Hilfsmethode zum Starten der Backgroundthreads
        /// </summary>
        private void StartThreads()
        {
            _updateUploads = new Thread(new ThreadStart(_updateUploads_ThreadWorker))
            {
                Name = "UpdateUploads",
                IsBackground = true
            };
            _updateUploads.Start();

            _checkConnectionToCore = new Thread(new ThreadStart(_checkConnectionToCore_ThreadWorker))
            {
                Name = "CheckConnectionToCore",
                IsBackground = true
            };
            _checkConnectionToCore.Start();

            _updateInformation = new Thread(new ThreadStart(_updateInformation_ThreadWorker))
            {
                Name = "UpdateInformation)",
                IsBackground = true
            };
            _updateInformation.Start();

            _updateDownloads = new Thread(new ThreadStart(_updateDownloads_ThreadWorker))
            {
                Name = "UpdateDownloads",
                IsBackground = true
            };
            _updateDownloads.Start();

            _listenArguments = new Thread(new ThreadStart(_listenArguments_ThreadWorker))
            {
                Name = "ListenArguments",
                IsBackground = true
            };
            _listenArguments.Start();

            _refreshServer = new Thread(new ThreadStart(_refreshServer_ThreadWorker))
            {
                Name = "RefreshServer",
                IsBackground = true
            };
            _refreshServer.Start();
        }

        /// <summary>
        /// Hilfsmethode zum Erzeugen der Objekte für Anzeige der Anfangsinformationen
        /// </summary>
        private void CreateSubObjects()
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                appleJuice.Information = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedInfos, config.UseCompression)) as AppleJuice).Information;
                appleJuice.NetworkInfo = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedInfos, config.UseCompression)) as AppleJuice).NetworkInfo;
                appleJuice.Ids = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedIds, config.UseCompression)) as AppleJuice).Ids;
                appleJuice.Upload = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedUploads, config.UseCompression)) as AppleJuice).Upload;
                appleJuice.Download = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedDownloads, config.UseCompression)) as AppleJuice).Download;
                appleJuice.User = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedUser, config.UseCompression)) as AppleJuice).User;
                appleJuice.Server = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedServer, config.UseCompression)) as AppleJuice).Server;
                appleJuice.Shares = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getShare, config.UseCompression)) as AppleJuice).Shares;
                appleJuice.GeneralInformation = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getInformation, config.UseCompression)) as AppleJuice).GeneralInformation;
            }
            nInfo = appleJuice.NetworkInfo;
            info = appleJuice.Information;
        }

        /// <summary>
        /// Hilfsmethode zum Setzen der HTTP-Requests
        /// </summary>
        private void SetXmlCalls()
        {
            getModifiedInfos = string.Format("/xml/modified.xml?timestamp=0&filter=informations&password={0}&mode=zip", config.Password);
            getModifiedDownloads = string.Format("/xml/modified.xml?timestamp=0&filter=down&password={0}&mode=zip", config.Password);
            getModifiedUploads = string.Format("/xml/modified.xml?timestamp=0&filter=uploads&password={0}&mode=zip", config.Password);
            getModifiedIds = string.Format("/xml/modified.xml?timestamp=0&filter=ids&password={0}&mode=zip", config.Password);
            getModifiedServer = string.Format("/xml/modified.xml?timestamp=0&filter=server&password={0}&mode=zip", config.Password);
            getModifiedUser = string.Format("/xml/modified.xml?timestamp=0&filter=user&password={0}&mode=zip", config.Password);
            getInformation = string.Format("/xml/information.xml?timestamp=0&password={0}&mode=zip", config.Password);
            getSearch = string.Format("/xml/modified.xml?timestamp=0&filter=search&password={0}&mode=zip", config.Password);
            getShare = string.Format("/xml/share.xml?timestamp=0&password={0}&mode=zip", config.Password);
            getSettings = string.Format("/xml/settings.xml?password={0}&mode=zip", config.Password);
        }

        /// <summary>
        /// Hilfsmethode zum erzeugen des UserDatagrids
        /// </summary>
        /// <param name="_users">Collection mit Inhalt der gefilterten User</param>
        private void CreateUserGrid(ObservableCollection<User> _users)
        {
            int index = -1;
            foreach (User singleUser in _users)
            {
                IEnumerable<User> getIndex = users.AsEnumerable().Where(a => a.Id.Equals(singleUser.Id));

                foreach (User _index in getIndex)
                {
                    index = users.IndexOf(_index);
                }

                if (index == -1)
                {
                    users.Add(singleUser);
                }
                else
                {
                    users[index].ActualDownloadPosition = singleUser.ActualDownloadPosition;
                    users[index].ActualFileSize = singleUser.ActualDownloadPosition - singleUser.DownloadFrom;
                    users[index].DirectState = singleUser.DirectState;
                    users[index].DownloadFrom = singleUser.DownloadFrom;
                    users[index].DownloadId = singleUser.DownloadId;
                    users[index].DownloadTo = singleUser.DownloadTo;
                    users[index].FileName = singleUser.FileName;
                    users[index].FileSize = singleUser.DownloadTo - singleUser.DownloadFrom;
                    users[index].FileSizeToGet = singleUser.DownloadTo - singleUser.ActualDownloadPosition; ;
                    users[index].Id = singleUser.Id;
                    users[index].NickName = singleUser.NickName;
                    users[index].OperatingSystem = singleUser.OperatingSystem;

                    if (singleUser.Status == 7)
                    {
                        users[index].Percentages = Math.Round((Convert.ToDouble(singleUser.ActualDownloadPosition - singleUser.DownloadFrom) / Convert.ToDouble(singleUser.DownloadTo - singleUser.DownloadFrom) * 100.00), 2) + " %";
                    }
                    else
                    {
                        users[index].Percentages = "0%";
                    }

                    users[index].PowerDownload = singleUser.PowerDownload;
                    users[index].QueuePosition = singleUser.QueuePosition;
                    users[index].Source = singleUser.Source;
                    users[index].Speed = singleUser.Speed;
                    users[index].Status = singleUser.Status;

                    if (users[index].Speed != 0)
                    {
                        users[index].TimeToEnd = Convert.ToInt32(users[index].FileSizeToGet) / users[index].Speed;
                    }
                    else
                    {
                        users[index].TimeToEnd = 0;
                    }

                    users[index].Version = singleUser.Version;
                }
            }

            for (int i = users.Count - 1; i >= 0; i--)
            {
                if (_users.Where(a => a.Id.Equals(users[i].Id)).Count() == 0)
                {
                    users.RemoveAt(i);
                }
            }
            users.Sort(a => a.FileName);
        }

        /// <summary>
        /// Hilfsmethode zum Refresh der aktiven Downloads
        /// </summary>
        private void CreateCurrentUploads()
        {
            try
            {
                IEnumerable<Upload> getCurrentUploads = appleJuice.Upload.AsEnumerable().Where(a => a.Status.Equals(1));

                List<Upload> _uploads = new List<Upload>();
                foreach (Upload upload in getCurrentUploads)
                {
                    _uploads.Add(upload);
                }

                foreach (Upload uLoads in _uploads)
                {
                    IEnumerable<Upload> getIndex = currentUploads.AsEnumerable().Where(a => a.Id.Equals(uLoads.Id));

                    int index = -1;
                    foreach (Upload _index in getIndex)
                    {
                        index = currentUploads.IndexOf(_index);
                    }

                    if (index != -1)
                    {
                        Share getFilename = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Id.Equals(uLoads.Shareid)).First();

                        currentUploads[index].ActualUploadPosition = uLoads.ActualUploadPosition;
                        currentUploads[index].Loaded = uLoads.Loaded;
                        currentUploads[index].UploadFrom = uLoads.UploadFrom;
                        currentUploads[index].UploadTo = uLoads.UploadTo;
                        currentUploads[index].Percentages = Math.Round((Convert.ToDouble(uLoads.ActualUploadPosition - uLoads.UploadFrom) / Convert.ToDouble(uLoads.UploadTo - uLoads.UploadFrom) * 100.00), 2) + " %";
                        currentUploads[index].Speed = uLoads.Speed;
                        currentUploads[index].WPercentages = Math.Round((Convert.ToDouble(uLoads.Loaded) / Convert.ToDouble(1) * 100.00), 2) + " %";
                        currentUploads[index].FileName = getFilename.ShortFileName;

                    }
                    else
                    {
                        IEnumerable<Share> getFilename = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Id.Equals(uLoads.Shareid));
                        foreach (Share fileName in getFilename)
                        {
                            currentUploads.Add(uLoads);
                            currentUploads[currentUploads.IndexOf(uLoads)].FileName = fileName.ShortFileName;
                            currentUploads[currentUploads.IndexOf(uLoads)].Percentages = Math.Round((Convert.ToDouble(uLoads.ActualUploadPosition - uLoads.UploadFrom) / Convert.ToDouble(uLoads.UploadTo - uLoads.UploadFrom) * 100.00), 2) + " %";
                            currentUploads[currentUploads.IndexOf(uLoads)].WPercentages = Math.Round((Convert.ToDouble(uLoads.Loaded) / Convert.ToDouble(1) * 100.00), 2) + " %";
                        }
                    }
                }

                for (int i = currentUploads.Count - 1; i >= 0; i--)
                {
                    if (_uploads.Where(a => a.Id.Equals(currentUploads[i].Id)).Count() == 0)
                    {
                        currentUploads.RemoveAt(i);
                    }
                }

                currentUploads.Sort(a => a.FileName);
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der momentanen Uploads!", ex);
            }
        }

        /// <summary>
        /// Hilfsmethode zum Refresh des Uploadqueues
        /// </summary>
        private void CreateWaitingUploads()
        {
            try
            {
                IEnumerable<Upload> getWaitingUploads = appleJuice.Upload.AsEnumerable().Where(a => a.Status != 1);

                List<Upload> _uploads = new List<Upload>();

                foreach (Upload upload in getWaitingUploads)
                {
                    _uploads.Add(upload);
                }

                foreach (Upload uLoads in _uploads)
                {
                    IEnumerable<Upload> getIndex = waitingUploads.AsEnumerable().Where(a => a.Id.Equals(uLoads.Id));

                    int index = -1;
                    foreach (Upload _index in getIndex)
                    {
                        index = waitingUploads.IndexOf(_index);
                    }

                    if (index != -1)
                    {
                        Share getFilename = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Id.Equals(uLoads.Shareid)).First();
                        waitingUploads[index].ActualUploadPosition = uLoads.ActualUploadPosition;
                        waitingUploads[index].Loaded = uLoads.Loaded;

                        if (uLoads.Status.Equals(2))
                        {
                            waitingUploads[index].Percentages = "0%";
                        }
                        else
                        {
                            waitingUploads[index].Percentages = Math.Round((Convert.ToDouble(uLoads.ActualUploadPosition - uLoads.UploadFrom) / Convert.ToDouble(uLoads.UploadTo - uLoads.UploadFrom) * 100.00), 2) + " %";
                        }

                        waitingUploads[index].Speed = uLoads.Speed;
                        waitingUploads[index].UploadFrom = uLoads.UploadFrom;
                        waitingUploads[index].UploadTo = uLoads.UploadTo;
                        waitingUploads[index].WPercentages = Math.Round((Convert.ToDouble(uLoads.Loaded) / Convert.ToDouble(1) * 100.00), 2) + " %";
                        waitingUploads[index].FileName = getFilename.ShortFileName;
                        waitingUploads[index].LastConnection = uLoads.LastConnection;
                    }
                    else
                    {
                        IEnumerable<Share> getFilename = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Id.Equals(uLoads.Shareid));
                        foreach (Share fileName in getFilename)
                        {
                            waitingUploads.Add(uLoads);
                            waitingUploads[waitingUploads.IndexOf(uLoads)].FileName = fileName.ShortFileName;
                            waitingUploads[waitingUploads.IndexOf(uLoads)].WPercentages = Math.Round((Convert.ToDouble(uLoads.Loaded) / Convert.ToDouble(1) * 100.00), 2) + " %";
                        }
                    }
                }

                for (int i = waitingUploads.Count() - 1; i >= 0; i--)
                {
                    if (_uploads.Where(a => a.Id.Equals(waitingUploads[i].Id)).Count() == 0)
                    {
                        waitingUploads.RemoveAt(i);
                    }
                }

                waitingUploads.Sort(a => a.FileName);
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der wartenden Uploads!", ex);
            }
        }
        #endregion

        #region DelegateMethods

        /// <summary>
        /// Delegatemethode zum erzeugen des Suchergebnisses
        /// </summary>
        /// <param name="searchEntrys">Vorhandene Sucheinträge</param>
        /// <param name="id">Id der Suche</param>
        private void _startSearch(ObservableCollection<SearchEntry> searchEntrys, int id)
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    // Suchergebnis nur EINMAL holen (frueher zweimal dieselbe URL).
                    AppleJuice searchResult = (AppleJuice)appleJuice.DeserializeToObj(webConnect.GetHttpResult(getSearch, config.UseCompression));
                    appleJuice.Search = searchResult.Search;
                    appleJuice.SearchEntry = searchResult.SearchEntry;

                    // Shares + Settings einmal pro Poll holen. Frueher lag das im
                    // tempSearch_CollectionChanged-Handler und lief damit pro hinzugefuegtem
                    // Treffer -> O(n) HTTP-Calls auf dem UI-Thread -> Programm fror beim Klick ein.
                    appleJuice.Shares = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getShare, config.UseCompression)) as AppleJuice).Shares;
                    settings = settings.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression)) as Settings;
                }

                Search _getSearch = appleJuice.Search.AsEnumerable().FirstOrDefault(a => a.id.Equals(id));
                if (_getSearch == null)
                {
                    return;
                }

                searchInfo.FoundFiles = searchEntrys.Count();
                searchInfo.Id = _getSearch.id;
                searchInfo.OpenSearches = _getSearch.OpenSearches;
                searchInfo.Running = _getSearch.Running;
                searchInfo.SumSearches = _getSearch.SumSearches;
                searchInfo.Users = appleJuice.NetworkInfo.Users;

                foreach (SearchEntry sEnt in appleJuice.SearchEntry)
                {
                    if (searchEntrys.Where(a => a.Id.Equals(sEnt.Id)).Count() == 0 && sEnt.SearchId.Equals(cTabItem.Tag))
                    {
                        searchEntrys.Add(sEnt);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim durchführen der Suche", ex);
            }
        }

        /// <summary>
        /// Delegatemethode zum Erneuern der Uploads
        /// </summary>
        private void RefreshUploadDatagrid()
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Upload = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedUploads, config.UseCompression)) as AppleJuice).Upload;
                }

                CreateCurrentUploads();
                CreateWaitingUploads();
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Refresh der Uploads", ex);
            }
        }



        /// <summary>
        /// Delegatmethode zum Erneuern der Informationen
        /// </summary>
        private void UpdateInformation()
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Information = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedInfos, config.UseCompression)) as AppleJuice).Information;
                    appleJuice.NetworkInfo = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedInfos, config.UseCompression)) as AppleJuice).NetworkInfo;
                }

                info.DownloadSpeed = appleJuice.Information.DownloadSpeed;
                info.Credits = appleJuice.Information.Credits;
                info.MaxUploadPositions = appleJuice.Information.MaxUploadPositions;
                info.OpenConnections = appleJuice.Information.OpenConnections;
                info.SessionDownload = appleJuice.Information.SessionDownload;
                info.SessionUpload = appleJuice.Information.SessionUpload;
                info.UploadSpeed = appleJuice.Information.UploadSpeed;

                nInfo.Files = appleJuice.NetworkInfo.Files;
                nInfo.FileSize = appleJuice.NetworkInfo.FileSize;
                nInfo.Firewalled = appleJuice.NetworkInfo.Firewalled;
                nInfo.Ip = appleJuice.NetworkInfo.Ip;
                nInfo.Users = appleJuice.NetworkInfo.Users;
                nInfo.WelcomeMessage = appleJuice.NetworkInfo.WelcomeMessage;

            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der Informationen!", ex);
            }
        }

        /// <summary>
        /// Delegatmethode zum Erneuern der Downloads
        /// </summary>
        private void RefreshDownloadDatagrid()
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Download = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedDownloads, config.UseCompression)) as AppleJuice).Download;
                }

                foreach (Download dLoad in appleJuice.Download)
                {
                    int getActiveUsers = appleJuice.User.AsEnumerable().Where(a => a.DownloadId.Equals(dLoad.Id) && a.Status.Equals(7)).Count();
                    int getDownloadUsers = appleJuice.User.AsEnumerable().Where(a => a.DownloadId.Equals(dLoad.Id) && (a.Status.Equals(5) || a.Status.Equals(7))).Count();
                    int getAllUsers = appleJuice.User.AsEnumerable().Where(a => a.DownloadId.Equals(dLoad.Id)).Count();
                    int getDownloadSpeed = appleJuice.User.AsEnumerable().Where(a => a.DownloadId.Equals(dLoad.Id) && a.Status.Equals(7)).Sum(a => a.Speed);

                    dLoad.Speed = getDownloadSpeed;
                    dLoad.ActiveUsers = getActiveUsers;
                    dLoad.DownloadUsers = getDownloadUsers;
                    dLoad.AllUsers = getAllUsers;

                    IEnumerable<Download> getIndex = downloads.AsEnumerable().Where(a => a.Id.Equals(dLoad.Id));

                    int index = -1;
                    foreach (Download _index in getIndex)
                    {
                        index = downloads.IndexOf(_index);
                    }

                    if (index != -1)
                    {
                        downloads[index].Speed = dLoad.Speed;
                        downloads[index].ActiveUsers = dLoad.ActiveUsers;
                        downloads[index].AllUsers = dLoad.AllUsers;
                        downloads[index].DownloadUsers = dLoad.DownloadUsers;
                        downloads[index].Status = dLoad.Status;
                        downloads[index].PowerDownload = dLoad.PowerDownload;
                        downloads[index].FileName = dLoad.FileName;
                        if (downloads[index].Speed != 0)
                        {
                            downloads[index].TimeToEnd = Convert.ToInt32(downloads[index].DownloadRest) / downloads[index].Speed;
                        }
                        else
                        {
                            downloads[index].TimeToEnd = 0;
                        }

                        if (dLoad.Status == 14)
                        {
                            downloads[index].DownloadRest = dLoad.Ready;
                            downloads[index].DownloadedFilesize = dLoad.Size;
                            downloads[index].CheckIfIsOver = dLoad.Size;
                            downloads[index].Percentages = Math.Round((Convert.ToDouble(dLoad.Size) / Convert.ToDouble(dLoad.Size) * 100.00), 2) + " %";
                        }
                        else
                        {
                            downloads[index].DownloadRest = (Convert.ToInt32(dLoad.Size) - Convert.ToInt32(dLoad.Ready)).ToString();
                            downloads[index].DownloadedFilesize = dLoad.Ready;
                            downloads[index].CheckIfIsOver = dLoad.Ready;
                            downloads[index].Percentages = Math.Round((Convert.ToDouble(dLoad.Ready) / Convert.ToDouble(dLoad.Size) * 100.00), 2) + " %";
                        }
                    }
                    else
                    {
                        downloads.Add(dLoad);
                        downloads.Sort(a => a.FileName);
                    }
                }

                if (downloads.Count > appleJuice.Download.Count)
                {
                    List<Download> tempDownload = new List<Download>();
                    IEnumerable<Download> getToDeleteDownloads = downloads.AsEnumerable().Where(a => a.Status.Equals(14) || a.Status.Equals(17));

                    foreach (Download tempVar in getToDeleteDownloads)
                    {
                        tempDownload.Add(tempVar);
                    }

                    foreach (Download dLoad in tempDownload)
                    {
                        downloads.Remove(dLoad);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der Downloads", ex);
            }
        }

        /// <summary>
        /// Delegatmethode zum erneuern der User
        /// </summary>
        private void RefreshUserDatagrid()
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.User = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedUser, config.UseCompression)) as AppleJuice).User;
                }

                ObservableCollection<User> temp = new ObservableCollection<User>();
                if (rBtnActive.IsChecked == true)
                {
                    IEnumerable<User> getUsers = appleJuice.User.Where(a => a.DownloadId == selectedDownload.Id && a.Status.Equals(7));

                    foreach (User _user in getUsers)
                    {
                        temp.Add(_user);
                    }

                    CreateUserGrid(temp);
                }
                if (rBtnWait.IsChecked == true)
                {
                    IEnumerable<User> getUsers = appleJuice.User.Where(a => a.DownloadId == selectedDownload.Id && a.Status.Equals(5));

                    foreach (User _user in getUsers)
                    {
                        temp.Add(_user);
                    }

                    CreateUserGrid(temp);
                }
                if (rBtnRest.IsChecked == true)
                {
                    IEnumerable<User> getUsers = appleJuice.User.Where(a => a.DownloadId == selectedDownload.Id && (a.Status != 5 && a.Status != 7));

                    foreach (User _user in getUsers)
                    {
                        temp.Add(_user);
                    }

                    CreateUserGrid(temp);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der Downloaduser!", ex);
            }
        }

        /// <summary>
        /// Delegatemethode zum Erneuern der Serverliste
        /// </summary>
        private void RefreshServerList()
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Server = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedServer, config.UseCompression)) as AppleJuice).Server;
                }

                Server getConnectedServer = appleJuice.Server.AsEnumerable().Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First();

                foreach (Server _server in appleJuice.Server)
                {
                    if (_server.Id.Equals(getConnectedServer.Id))
                    {
                        _server.IsConnected = true;
                    }
                    else
                    {
                        _server.IsConnected = false;
                    }

                    int index = -1;
                    IEnumerable<Server> getIndex = server.AsEnumerable().Where(a => a.Id.Equals(_server.Id));

                    foreach (Server _index in getIndex)
                    {
                        index = server.IndexOf(_index);
                    }

                    if (index != -1)
                    {
                        server[index].ConnectionTry = _server.ConnectionTry;
                        server[index].Host = _server.Host;
                        server[index].Id = _server.Id;
                        server[index].IsConnected = _server.IsConnected;
                        server[index].LastSeen = _server.LastSeen;
                        server[index].Name = _server.Name;
                        server[index].Port = _server.Port;
                    }
                    else
                    {
                        server.Add(_server);
                    }
                }

                if (server.Count > appleJuice.Server.Count)
                {
                    Server _server = new Server();


                    for (int i = 0; i < server.Count; i++)
                    {
                        int getServer = appleJuice.Server.AsEnumerable().Where(a => a.Id == server[i].Id).Count();

                        if (getServer == 0)
                        {
                            _server = server[i];
                        }
                    }

                    server.Remove(_server);
                }
                server.Sort(a => a.Name);
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der Server!", ex);
            }
        }

        /// <summary>
        /// Obergrenze fuer die Quellenanzahl-Faerbung; ab hier keine sichtbare Steigerung mehr.
        /// </summary>
        private const int MaxGradientSources = 8;

        /// <summary>
        /// Delegatmethode zum Erzeugen/Erneuern der Downloadpartliste
        /// </summary>
        private void CreateDownloadPartlist()
        {
            try
            {
                string getDownloadPartList = string.Format("/xml/downloadpartlist.xml?id={0}&password={1}&mode=zip", selectedDownload.Id, config.Password);

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Parts = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getDownloadPartList, config.UseCompression)) as AppleJuice).Parts;
                    appleJuice.FileInformation = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getDownloadPartList, config.UseCompression)) as AppleJuice).FileInformation;
                }

                IEnumerable<User> activeSources = users.Where(a => a.Status.Equals(7));

                int width = (int)Math.Max(1, imgPartList.ActualWidth);
                int height = (int)Math.Max(1, imgPartList.ActualHeight);

                WriteableBitmap bitmap = RenderPartList(appleJuice.FileInformation.Filesize, appleJuice.Parts, activeSources, width, height, isMainList: true);
                if (bitmap != null)
                {
                    imgPartList.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erstellen der DownloadPartListe!", ex);
            }
        }

        /// <summary>
        /// Delegatmethode zum Erzeugen/Erneuern der Userpartliste
        /// </summary>
        private void CreateUserPartlist()
        {
            try
            {
                string getUserPartList = string.Format("/xml/userpartlist.xml?id={0}&password={1}&mode=zip", selectedUser.Id, config.Password);

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Parts = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getUserPartList, config.UseCompression)) as AppleJuice).Parts;
                    appleJuice.FileInformation = (appleJuice.DeserializeToObj(webConnect.GetHttpResult(getUserPartList, config.UseCompression)) as AppleJuice).FileInformation;
                }

                int width = (int)Math.Max(1, imgPartList.ActualWidth);
                int height = (int)Math.Max(1, imgPartList.ActualHeight);

                WriteableBitmap bitmap = RenderPartList(appleJuice.FileInformation.Filesize, appleJuice.Parts, null, width, height, isMainList: false);
                if (bitmap != null)
                {
                    imgPartList.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erstellen der UserPartList!", ex);
            }
        }

        /// <summary>
        /// Rendert eine Part-/Verfuegbarkeitsliste wie das offizielle appleJuice-GUI: die Datei
        /// wird ueber mehrere Zeilen umgebrochen (Zeile 0 = erstes Stueck, Zeile 1 = naechstes
        /// usw.), wodurch die Aufloesung mit der Anzeigeflaeche skaliert (groessenabhaengig).
        /// Es wird ein <c>width x zeilen</c>-Bitmap erzeugt; die vertikale Streckung auf die
        /// volle Hoehe (jede Zeile -&gt; ca. 15 px) uebernimmt das Stretch="Fill" der Image-Control.
        /// Jeder Part gilt von seiner FromPosition bis zur FromPosition des naechsten Parts
        /// (letzter bis Dateiende) - so gibt es der Core die Liste vor.
        /// </summary>
        private WriteableBitmap RenderPartList(long fileSize, List<Part> parts, IEnumerable<User> activeSources, int width, int height, bool isMainList)
        {
            if (width <= 0 || height <= 0 || fileSize <= 0 || parts == null || parts.Count == 0)
            {
                return null;
            }

            // Mehrzeilige Packung: eine 15px-Zeile pro sichtbarer Textzeile, die Datei laeuft
            // ueber alle Zeilen als ein durchgehender Streifen (width * rows Spalten insgesamt).
            const int rowHeight = 15;
            int rows = Math.Max(1, height / rowHeight);
            int totalColumns = rows * width;

            int[] strip = new int[totalColumns];

            // Byte-Position -> Spaltenindex im Gesamtstreifen. Proportional in long-Arithmetik;
            // deckt grosse wie sehr kleine Dateien ab (kein separater "miniFile"-Sonderfall noetig).
            int ColumnForByte(long bytePosition)
            {
                long column = bytePosition * totalColumns / fileSize;
                return (int)Math.Max(0, Math.Min(totalColumns - 1, column));
            }

            // Verfuegbarkeit: jeder Part faerbt [FromPosition, naechste FromPosition).
            for (int i = 0; i < parts.Count; i++)
            {
                long from = parts[i].FromPosition;
                long to = (i + 1 < parts.Count) ? parts[i + 1].FromPosition : fileSize;

                int fromColumn = ColumnForByte(from);
                int toColumn = (to >= fileSize) ? totalColumns : ColumnForByte(to);

                int color = ToArgb(GetColorForType(parts[i].type, isMainList));
                for (int column = fromColumn; column < toColumn; column++)
                {
                    strip[column] = color;
                }
            }

            // Aktive Uebertragungen (nur Hauptliste) ueberlagern: Orange fuer den bereits
            // geladenen Bereich, Gelb als Positionsmarker (passend zur Legende geladen/aktiv).
            if (isMainList && activeSources != null)
            {
                int orange = ToArgb(Colors.Orange);
                int yellow = ToArgb(Colors.Yellow);

                foreach (User user in activeSources)
                {
                    int fromColumn = ColumnForByte(user.DownloadFrom);
                    int positionColumn = ColumnForByte(user.ActualDownloadPosition);

                    for (int column = fromColumn; column < positionColumn; column++)
                    {
                        strip[column] = orange;
                    }

                    strip[positionColumn] = yellow;
                }
            }

            // Streifen als width x rows umbrechen (row-major passt direkt zu WritePixels);
            // die Image-Control streckt das Bild anschliessend vertikal auf die volle Hoehe.
            WriteableBitmap bitmap = new WriteableBitmap(width, rows, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, rows), strip, width * 4, 0);
            return bitmap;
        }

        private static Color GetColorForType(int type, bool isMainList)
        {
            if (type == -1)
            {
                // Hauptliste: fertig/ueberprueft (Legende "colorfinished"); Quellliste: bei dieser
                // Quelle vorhanden (Legende "coloravailable") - bestehendes Apfelmus-Farbschema.
                return isMainList ? Colors.Green : Colors.Blue;
            }

            if (type <= 0)
            {
                return Colors.Red;
            }

            return SourceCountToColor(type);
        }

        /// <summary>
        /// Blauton fuer die Anzahl verfuegbarer Quellen: je mehr Quellen, desto dunkler/
        /// gesaettigter das Blau. Formelbasiert statt einer festen Farbtabelle, gedeckelt bei
        /// <see cref="MaxGradientSources"/>.
        /// </summary>
        private static Color SourceCountToColor(int sourceCount)
        {
            int clamped = Math.Max(1, Math.Min(sourceCount, MaxGradientSources));
            double t = (clamped - 1) / (double)(MaxGradientSources - 1);
            byte channel = (byte)Math.Round(220 - (t * 190));
            return Color.FromRgb(channel, channel, 255);
        }

        private static int ToArgb(Color c)
        {
            return (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
        }
        #endregion

        #region Eventhandler

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Server server = new Server();
                foreach (Server serv in appleJuice.Server)
                {
                    if (serv.Id.Equals(nInfo.ConnectedWithServerId))
                    {
                        server = serv;
                    }
                }

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    settings = settings.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression)) as Settings;
                }

                GenerateFirstTreeItem();

                foreach (SettingsDirectory sDir in settings.share.Directory)
                {
                    StackPanel sPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal
                    };

                    CheckBox cBox = new CheckBox
                    {
                        IsChecked = true
                    };
                    cBox.Unchecked += new RoutedEventHandler(cBox_UnChecked);

                    TextBlock tBlock = new TextBlock
                    {
                        Text = sDir.Name,
                        Margin = new Thickness(5, 0, 0, 0)
                    };

                    sPanel.Children.Add(cBox);
                    sPanel.Children.Add(tBlock);

                    sPanelShareFolders.Children.Add(sPanel);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler nach geladenem Fenster!", ex);
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tbxDownload.Text.Contains("ajfsp"))
                {
                    ProcessLink();
                    tbxDownload.Text = string.Empty;
                    tabDownload.IsSelected = true;
                }
                else
                {
                    tbxDownload.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Übernehmen des AJ-Links als Text!", ex);
            }
        }

        private void btnPowerDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Download> _tempDownloadList = new List<Download>();

                if (dGridDownloads.SelectedItems.Count == 0)
                {
                    return;
                }

                foreach (Download _dLoad in dGridDownloads.SelectedItems)
                {
                    _tempDownloadList.Add(_dLoad);
                }

                string action = string.Empty;

                if (_tempDownloadList.Count > 1)
                {
                    string ids = string.Empty;
                    for (int i = 0; i < _tempDownloadList.Count; i++)
                    {
                        if (i == 0)
                        {
                            ids = string.Format("id={0}", _tempDownloadList[i].Id);
                        }
                        else
                        {
                            ids += string.Format("&id{0}={1}", i, _tempDownloadList[i].Id);
                        }
                    }

                    action = string.Format("/function/setpowerdownload?{0}&Powerdownload={1}&password={2}", ids, ((KeyValuePair<int, double>)cbxPowerDownload.SelectedItem).Key, config.Password);

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
                else
                {
                    foreach (Download dLoad in _tempDownloadList)
                    {
                        action = string.Format("/function/setpowerdownload?id={0}&Powerdownload={1}&password={2}", dLoad.Id.ToString(), ((KeyValuePair<int, double>)cbxPowerDownload.SelectedItem).Key, config.Password);
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Setzen des Powerdownloads!", ex);
            }
        }

        private void btnShareRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Shares = ((AppleJuice)appleJuice.DeserializeToObj(webConnect.GetHttpResult(getShare, false))).Shares;
                }

                char[] sep = new char[] { Convert.ToChar(appleJuice.GeneralInformation.FileSystem.Seperator) };

                foreach (Share _share in appleJuice.Shares.Share)
                {
                    int index = -1;

                    IEnumerable<Share> getIndex = _shares.AsEnumerable().Where(a => a.Id.Equals(_share.Id));
                    string[] tempStr = _share.FileName.Replace(_share.ShortFileName, string.Empty).Split(sep, StringSplitOptions.RemoveEmptyEntries);


                    foreach (Share _index in getIndex)
                    {
                        index = _shares.IndexOf(_index);
                    }

                    if (index != -1)
                    {
                        _shares[index].AskCount = _share.AskCount;
                        _shares[index].CheckSum = _share.CheckSum;
                        _shares[index].FileName = _share.FileName;
                        _shares[index].Id = _share.Id;
                        _shares[index].LastAsked = _share.LastAsked;
                        _shares[index].Priority = _share.Priority;
                        _shares[index].SearchCount = _share.SearchCount;
                        _shares[index].ShortFileName = _share.ShortFileName;
                        _shares[index].Size = _share.Size;
                        if (_share.FileName.Contains(".data"))
                        {
                            _shares[index].Path = tempStr[tempStr.Length - 2];
                        }
                        else
                        {
                            _shares[index].Path = tempStr[tempStr.Length - 1];
                        }
                    }
                    else
                    {
                        if (_share.FileName.Contains(".data"))
                        {
                            _share.Path = tempStr[tempStr.Length - 2];
                        }
                        else
                        {
                            _share.Path = tempStr[tempStr.Length - 1];
                        }

                        _shares.Add(_share);
                    }
                }

                using (lcvShares.DeferRefresh())
                {
                    lcvShares.SortDescriptions.Clear();
                    lcvShares.SortDescriptions.Add(sdShares);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erzeugen der Shareliste!", ex);
            }
        }

        private void btnSetPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int priorityValue = 1000;

                List<Share> _tempShareList = new List<Share>();

                if (dGridShares.SelectedItems.Count == 0)
                {
                    return;
                }

                int getPriorityValue = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Priority > 1).Select(a => a.Priority).Sum();

                priorityValue -= getPriorityValue;

                foreach (Share _share in dGridShares.SelectedItems)
                {
                    _tempShareList.Add(_share);
                }

                string action = string.Empty;

                if (priorityValue != 0)
                {
                    if (_tempShareList.Count > 1)
                    {
                        string ids = string.Empty;
                        for (int i = 0; i < _tempShareList.Count; i++)
                        {
                            if (i == 0)
                            {
                                ids = string.Format("id={0}", _tempShareList[i].Id);
                            }
                            else
                            {
                                ids += string.Format("&id{0}={1}", i, _tempShareList[i].Id);
                            }
                        }

                        if (priorityValue - ((KeyValuePair<int, int>)cbxPriority.SelectedItem).Value > 0)
                        {
                            action = "/function/setpriority?" + ids + "&priority=" + ((KeyValuePair<int, int>)cbxPriority.SelectedItem).Key + "&password=" + config.Password;
                        }
                        else
                        {
                            action = "/function/setpriority?" + ids + "&priority=" + priorityValue + "&password=" + config.Password;
                        }

                        using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                        {
                            webConnect.StartXMLFunction(action);
                        }
                    }
                    else
                    {
                        foreach (Share _share in _tempShareList)
                        {
                            action = "/function/setpriority?id=" + _share.Id.ToString() + "&priority=" + ((KeyValuePair<int, int>)cbxPriority.SelectedItem).Key + "&password=" + config.Password;
                        }

                        using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                        {
                            webConnect.StartXMLFunction(action);
                        }
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        appleJuice.Shares = ((AppleJuice)appleJuice.DeserializeToObj(webConnect.GetHttpResult(getShare, false))).Shares;
                    }

                    foreach (Share _share in _tempShareList)
                    {
                        IEnumerable<Share> getShare = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Id.Equals(_share.Id));
                        foreach (Share _tempShare in getShare)
                        {
                            _share.Priority = _tempShare.Priority;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Setzen der Priorität!", ex);
            }
        }

        private void btnDelPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Share> _tempShareList = new List<Share>();

                if (dGridShares.SelectedItems.Count == 0)
                {
                    return;
                }

                foreach (Share _share in dGridShares.SelectedItems)
                {
                    _tempShareList.Add(_share);
                }

                string action = string.Empty;


                if (_tempShareList.Count > 1)
                {
                    string ids = string.Empty;
                    for (int i = 0; i < _tempShareList.Count; i++)
                    {
                        if (i == 0)
                        {
                            ids = string.Format("id={0}", _tempShareList[i].Id);
                        }
                        else
                        {
                            ids += string.Format("&id{0}={1}", i, _tempShareList[i].Id);
                        }
                    }


                    action = "/function/setpriority?" + ids + "&priority=1&password=" + config.Password;

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
                else
                {
                    foreach (Share _share in _tempShareList)
                    {
                        action = "/function/setpriority?id=" + _share.Id.ToString() + "&priority=1&password=" + config.Password;
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    appleJuice.Shares = ((AppleJuice)appleJuice.DeserializeToObj(webConnect.GetHttpResult(getShare, false))).Shares;
                }

                foreach (Share _share in _tempShareList)
                {
                    IEnumerable<Share> getShare = appleJuice.Shares.Share.AsEnumerable().Where(a => a.Id.Equals(_share.Id));
                    foreach (Share _tempShare in getShare)
                    {
                        _share.Priority = _tempShare.Priority;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Löschen der Priorität!", ex);
            }
        }

        private void btnShareCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int countShare = 0;
                string shareDirs = string.Empty;
                string shareSubs = string.Empty;
                string action = string.Empty;
                Settings _settings = new Settings();
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    _settings = (Settings)_settings.DeserializeToObj(webConnect.GetHttpResult("/xml/settings.xml?password=" + config.Password + "&mode=zip", config.UseCompression));
                }

                string[] dirs = new string[_settings.share.Directory.Count];
                bool[] sharemode = new bool[_settings.share.Directory.Count];

                for (int i = 0; i < _settings.share.Directory.Count; i++)
                {
                    dirs[i] = _settings.share.Directory[i].Name;
                    if (_settings.share.Directory[i].ShareMode == "subdirectory")
                    {
                        sharemode[i] = true;
                    }
                    else
                    {
                        sharemode[i] = false;
                    }

                    countShare += 1;
                    shareDirs += string.Format("&sharedirectory{0}={1}", i + 1, dirs[i].Replace(" ", "%20"));
                    shareSubs += string.Format("&sharesub{0}={1}", i + 1, sharemode[i]);
                }

                action = string.Format("/function/setsettings?countshares={0}{1}{2}&password={3}", countShare, shareDirs, shareSubs, config.Password);

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    webConnect.StartXMLFunction(action);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler bei der Shareüberprüfung!", ex);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow sWindow = new SettingsWindow(config, Resources)
            {
                Owner = this
            };
            sWindow.ShowDialog();
        }

        private void dGridDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_updateUsers != null)
                {
                    if ((_updateUsers.ThreadState & ThreadState.Running) == ThreadState.Running)
                    {
                        _updateUsers = null;
                    }
                }

                if (_refreshUserPartList != null)
                {
                    if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                    {
                        _refreshUserPartList = null;
                    }
                }

                if (_refreshDownloadPartList != null)
                {
                    if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                    {
                        _refreshDownloadPartList = null;
                    }
                }

                imgPartList.Source = null;

                if (users.Count > 0)
                {
                    users.Clear();
                }

                selectionDownloadGrid = (DataGrid)sender;
                selectedDownload = (Download)selectionDownloadGrid.SelectedItem;

                _updateUsers = new Thread(new ThreadStart(_updateUsers_ThreadStart))
                {
                    Name = "UpdateUsers",
                    IsBackground = true
                };
                _updateUsers.Start();

                if (dGridDownloads.Items.Count > 0 && selectedDownload != null)
                {
                    if (cBoxPartList.IsChecked == true)
                    {
                        if (_refreshDownloadPartList != null)
                        {
                            if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                            {
                                _refreshDownloadPartList = null;
                            }
                        }

                        _refreshDownloadPartList = new Thread(new ThreadStart(_refreshDownloadPartList_ThreadWorker))
                        {
                            Name = "RefreshDownloadPartlist",
                            IsBackground = true
                        };
                        _refreshDownloadPartList.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Wechsel des Downloaditems", ex);
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Download> _tempDownloadList = new List<Download>();

                if (dGridDownloads.SelectedItems.Count == 0)
                {
                    return;
                }

                foreach (Download _dLoad in dGridDownloads.SelectedItems)
                {
                    _tempDownloadList.Add(_dLoad);
                }

                string action = string.Empty;

                if (_tempDownloadList.Count > 1)
                {
                    string ids = string.Empty;
                    for (int i = 0; i < _tempDownloadList.Count; i++)
                    {
                        if (i == 0)
                        {
                            ids = string.Format("id={0}", _tempDownloadList[i].Id);
                        }
                        else
                        {
                            ids += string.Format("&id{0}={1}", i, _tempDownloadList[i].Id);
                        }
                    }

                    action = "/function/resumedownload?" + ids + "&password=" + config.Password;

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
                else
                {
                    foreach (Download dLoad in _tempDownloadList)
                    {
                        action = "/function/resumedownload?id=" + dLoad.Id.ToString() + "&password=" + config.Password;
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Fortsetzen des Downloads!", ex);
            }
        }

        private void Break_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Download> _tempDownloadList = new List<Download>();

                if (dGridDownloads.SelectedItems.Count == 0)
                {
                    return;
                }

                foreach (Download _dLoad in dGridDownloads.SelectedItems)
                {
                    _tempDownloadList.Add(_dLoad);
                }

                string action = string.Empty;

                if (_tempDownloadList.Count > 1)
                {
                    string ids = string.Empty;
                    for (int i = 0; i < _tempDownloadList.Count; i++)
                    {
                        if (i == 0)
                        {
                            ids = string.Format("id={0}", _tempDownloadList[i].Id);
                        }
                        else
                        {
                            ids += string.Format("&id{0}={1}", i, _tempDownloadList[i].Id);
                        }
                    }

                    action = "/function/pausedownload?" + ids + "&password=" + config.Password;

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
                else
                {
                    foreach (Download dLoad in _tempDownloadList)
                    {
                        action = "/function/pausedownload?id=" + dLoad.Id.ToString() + "&password=" + config.Password;
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Pausieren des Downloads!", ex);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_refreshDownloadPartList != null)
                {
                    if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                    {
                        _refreshDownloadPartList = null;
                    }
                }

                if (_refreshUserPartList != null)
                {
                    if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                    {
                        _refreshUserPartList = null;
                    }
                }

                imgPartList.Source = null;

                List<Download> _tempDownloadList = new List<Download>();

                if (dGridDownloads.SelectedItems.Count == 0)
                {
                    return;
                }

                foreach (Download _dLoad in dGridDownloads.SelectedItems)
                {
                    _tempDownloadList.Add(_dLoad);
                }

                string action = string.Empty;

                if (_tempDownloadList.Count > 1)
                {
                    string ids = string.Empty;
                    for (int i = 0; i < _tempDownloadList.Count; i++)
                    {
                        if (i == 0)
                        {
                            ids = string.Format("id={0}", _tempDownloadList[i].Id);
                        }
                        else
                        {
                            ids += string.Format("&id{0}={1}", i, _tempDownloadList[i].Id);
                        }
                    }

                    action = "/function/canceldownload?" + ids + "&password=" + config.Password;

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
                else
                {
                    foreach (Download dLoad in _tempDownloadList)
                    {
                        action = "/function/canceldownload?id=" + dLoad.Id.ToString() + "&password=" + config.Password;
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(action);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Abbrechen des Downloads!", ex);
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Download dLoad = dGridDownloads.SelectedItem as Download;

                RenameDownloadWindow rDown = new RenameDownloadWindow(config, dLoad, Resources)
                {
                    Owner = this
                };
                rDown.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Umbenennen des Downloads", ex);
            }
        }

        private void TargetFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TargetdirWindow tWindow = new TargetdirWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (tWindow.ShowDialog().Equals(true))
                {
                    if (dGridDownloads.SelectedItems.Count > 1)
                    {
                        foreach (var item in dGridDownloads.SelectedItems)
                        {
                            string setTargetDir = string.Format("/function/settargetdir?id={0}&dir={1}&password={2}", (item as Download).Id, tWindow.TargetDir, config.Password);

                            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                            {
                                webConnect.StartXMLFunction(setTargetDir);
                            }
                        }
                    }
                    else
                    {
                        string setTargetDir = string.Format("/function/settargetdir?id={0}&dir={1}&password={2}", (dGridDownloads.SelectedItem as Download).Id, tWindow.TargetDir, config.Password);

                        using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                        {
                            webConnect.StartXMLFunction(setTargetDir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Anlegen des Zielordners", ex);
            }
        }

        private void Finished_Click(object sender, RoutedEventArgs e)
        {
            if (_refreshDownloadPartList != null)
            {
                if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshDownloadPartList = null;
                }
            }

            if (_refreshUserPartList != null)
            {
                if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshUserPartList = null;
                }
            }

            imgPartList.Source = null;

            string action = string.Format("/function/cleandownloadlist?password={0}", config.Password);

            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                webConnect.StartXMLFunction(action);
            }
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Download tmpDownload = (Download)dGridDownloads.SelectedItem;

                if (tmpDownload == null)
                {
                    return;
                }

                string txt = string.Format("ajfsp://file|{0}|{1}|{2}/", tmpDownload.FileName, tmpDownload.Hash, tmpDownload.Size);
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Source_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Download tmpDownload = (Download)dGridDownloads.SelectedItem;

                if (tmpDownload == null)
                {
                    return;
                }

                string getSettings = "/xml/settings.xml?password=" + config.Password + "&mode=zip";
                Settings set = new Settings();
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    set = (Settings)set.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression));
                }
                string txt = string.Format("ajfsp://file|{0}|{1}|{2}|{3}:{4}:{5}:{6}/", tmpDownload.FileName, tmpDownload.Hash, tmpDownload.Size, appleJuice.NetworkInfo.Ip, set.Port, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Host, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Port);
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShareLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                Share share = (Share)dGridShares.SelectedItem;

                if (share == null)
                {
                    return;
                }

                string txt = string.Format("ajfsp://file|{0}|{1}|{2}/\n", share.ShortFileName, share.CheckSum, share.Size);
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Clipboard.SetText(txt);
                        return;
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShareLinks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string txt = string.Empty;

                foreach (Share share in dGridShares.SelectedItems)
                {


                    if (share == null)
                    {
                        return;
                    }

                    txt += string.Format("ajfsp://file|{0}|{1}|{2}/\n", share.ShortFileName, share.CheckSum, share.Size);

                }
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Clipboard.SetText(txt);
                        return;
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShareSource_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Share share = (Share)dGridShares.SelectedItem;

                if (share == null)
                {
                    return;
                }

                string getSettings = "/xml/settings.xml?password=" + config.Password + "&mode=zip";
                Settings set = new Settings();
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    set = (Settings)set.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression));
                }

                string txt = string.Format("ajfsp://file|{0}|{1}|{2}|{3}:{4}:{5}:{6}/", share.ShortFileName, share.CheckSum, share.Size, appleJuice.NetworkInfo.Ip, set.Port, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Host, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Port);
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShareSources_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string txt = string.Empty;

                foreach (Share share in dGridShares.SelectedItems)
                {

                    if (share == null)
                    {
                        return;
                    }

                    string getSettings = "/xml/settings.xml?password=" + config.Password + "&mode=zip";
                    Settings set = new Settings();
                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        set = (Settings)set.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression));
                    }

                    txt += string.Format("ajfsp://file|{0}|{1}|{2}|{3}:{4}:{5}:{6}/\n", share.ShortFileName, share.CheckSum, share.Size, appleJuice.NetworkInfo.Ip, set.Port, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Host, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Port);
                }
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void SelectDataGridRow(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dGridShares.Items.Count > 0)
                {
                    if (Regex.IsMatch((e as KeyEventArgs).Key.ToString(), @"^[a-zA-Z]+$"))
                    {
                        foreach (Share share in dGridShares.Items)
                        {
                            if (!(dGridShares.SelectedItem as Share).ShortFileName.StartsWith((e as KeyEventArgs).Key.ToString()))
                            {
                                if (share.ShortFileName.StartsWith((e as KeyEventArgs).Key.ToString()))
                                {
                                    dGridShares.SelectedItem = share;
                                    dGridShares.ScrollIntoView(share);
                                    break;
                                }
                            }
                            else
                            {
                                dGridShares.SelectedIndex += 1;
                                dGridShares.ScrollIntoView(dGridShares.SelectedItem);
                                break;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void SearchLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseableTab.CloseableTabItem tempTab = (CloseableTab.CloseableTabItem)tControlSearches.SelectedItem;
                DataGrid dGrid = tempTab.Content as DataGrid;
                SearchEntry sEnt = dGrid.SelectedItem as SearchEntry;

                if (sEnt == null)
                {
                    return;
                }

                string txt = string.Format("ajfsp://file|{0}|{1}|{2}/", sEnt.FileName.Name, sEnt.Checksum, sEnt.Size);
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void SearchSource_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseableTab.CloseableTabItem tempTab = (CloseableTab.CloseableTabItem)tControlSearches.SelectedItem;
                DataGrid dGrid = tempTab.Content as DataGrid;
                SearchEntry sEnt = dGrid.SelectedItem as SearchEntry;

                if (sEnt == null)
                {
                    return;
                }

                string getSettings = "/xml/settings.xml?password=" + config.Password + "&mode=zip";
                Settings set = new Settings();
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    set = (Settings)set.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression));
                }

                string txt = string.Format("ajfsp://file|{0}|{1}|{2}|{3}:{4}:{5}:{6}/", sEnt.FileName.Name, sEnt.Checksum, sEnt.Size, appleJuice.NetworkInfo.Ip, set.Port, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Host, server.Where(a => a.Id.Equals(appleJuice.NetworkInfo.ConnectedWithServerId)).First().Port);
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void CloseTab(object sender, RoutedEventArgs e)
        {
            TabItem tItem = e.Source as TabItem;
            if (tItem != null)
            {

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    webConnect.StartXMLFunction("/function/cancelsearch?id=" + tItem.Tag + "&password=" + config.Password);
                }

                TabControl tabControl = tItem.Parent as TabControl;
                if (tabControl != null)
                {
                    tabControl.Items.Remove(tItem);
                }
            }

            if (_searchThread != null && _searchThread.IsAlive && _searchThread.Name.Contains(tItem.Tag.ToString()))
            {
                // Kooperativ beenden statt Thread.Abort() (existiert unter .NET Core nicht mehr).
                _stopSearch = true;
                _activateButtons();
            }
        }

        private void DownloadSearchResult_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string link = string.Empty;
                CloseableTab.CloseableTabItem tempTab = (CloseableTab.CloseableTabItem)tControlSearches.SelectedItem;
                DataGrid dGrid = tempTab.Content as DataGrid;
                List<SearchEntry> sEntry = new List<SearchEntry>();
                string _FileName;
                foreach (SearchEntry tmp in dGrid.SelectedItems)
                {
                    sEntry.Add(tmp);
                }

                if (sEntry.Count > 0)
                {
                    foreach (SearchEntry sTemp in sEntry)
                    {
                        if (sTemp.FileName.Name.Contains(" "))
                        {
                            _FileName = sTemp.FileName.Name.Replace(" ", "%20");
                        }
                        else
                        {
                            _FileName = sTemp.FileName.Name;
                        }

                        link = string.Format("ajfsp://file|{0}|{1}|{2}/", _FileName, sTemp.Checksum, sTemp.Size);

                        string processLink = "/function/processlink?link=" + link + "&password=" + config.Password;

                        using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                        {
                            webConnect.StartXMLFunction(processLink);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler bei der Übernahme des AJ-Links!", ex);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Share _tempShare = (Share)dGridShares.SelectedItem;
        }

        private void TitleBarMinimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void TitleBarMaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void TitleBarClose_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (appleJuice.Search != null && appleJuice.Search.Count > 0)
            {
                foreach (Search tempSearch in appleJuice.Search)
                {
                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        string action = string.Format("/function/cancelsearch?id={0}&password={1}", tempSearch.id, config.Password);
                        webConnect.StartXMLFunction(action);
                    }
                }
            }
        }

        private void CoreExit_Click(object sender, RoutedEventArgs e)
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                string action = "/function/exitcore?password=" + config.Password;
                webConnect.StartXMLFunction(action);
            }
        }

        private void ConnectServer_Click(object sender, RoutedEventArgs e)
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                appleJuice.NetworkInfo = ((AppleJuice)appleJuice.DeserializeToObj(webConnect.GetHttpResult(getModifiedInfos, config.UseCompression))).NetworkInfo;
            }
            System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // den Timestamp addieren           
            dateTime = dateTime.AddMilliseconds(appleJuice.NetworkInfo.ConnectedSince);
            dateTime = dateTime.ToLocalTime();
            TimeSpan tSpan = DateTime.Now.Subtract(dateTime);
            if (tSpan.TotalMinutes <= 30.0)
            {

                if (MessageBox.Show(Resources["connectmessage"].ToString(), "Information", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        string action = "/function/serverlogin?id=" + ((Server)dGridServer.SelectedItem).Id + "&password=" + config.Password;
                        webConnect.StartXMLFunction(action);
                    }
                }
            }
            else
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    string action = "/function/serverlogin?id=" + ((Server)dGridServer.SelectedItem).Id + "&password=" + config.Password;
                    webConnect.StartXMLFunction(action);
                }
            }
        }

        private void RemoveServer_Click(object sender, RoutedEventArgs e)
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                string action = "/function/removeserver?id=" + ((Server)dGridServer.SelectedItem).Id + "&password=" + config.Password;
                webConnect.StartXMLFunction(action);
            }
        }

        private void GetServer_Click(object sender, RoutedEventArgs e)
        {
            Serverlist serverList = WebConnect.GetXMLServerList();

            foreach (var server in serverList.Server)
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    webConnect.StartXMLFunction("/function/processlink?link=" + server.Link + "&password=" + config.Password);
                }
            }
        }

        private void German_Click(object sender, RoutedEventArgs e)
        {
            Resources.MergedDictionaries.RemoveAt(0);
            dict = new ResourceDictionary();
            config.LanguageFile = "..\\Resourcen\\DictionaryGerman.xaml";
            dict.Source = new Uri(config.LanguageFile, UriKind.Relative);
            Resources.MergedDictionaries.Add(dict);
            ConfigSerializer.SerializeToFile(config);
        }

        private void English_Click(object sender, RoutedEventArgs e)
        {
            Resources.MergedDictionaries.RemoveAt(0);
            dict = new ResourceDictionary();
            config.LanguageFile = "..\\Resourcen\\DictionaryEnglish.xaml";
            dict.Source = new Uri(config.LanguageFile, UriKind.Relative);
            Resources.MergedDictionaries.Add(dict);
            ConfigSerializer.SerializeToFile(config);
        }

        private void Italian_Click(object sender, RoutedEventArgs e)
        {
            Resources.MergedDictionaries.RemoveAt(0);
            dict = new ResourceDictionary();
            config.LanguageFile = "..\\Resourcen\\DictionaryItalian.xaml";
            dict.Source = new Uri(config.LanguageFile, UriKind.Relative);
            Resources.MergedDictionaries.Add(dict);
            ConfigSerializer.SerializeToFile(config);
        }

        private void Dark_Click(object sender, RoutedEventArgs e)
        {
            config.Theme = ThemeManager.Dark;
            ThemeManager.Apply(config.Theme);
            ConfigSerializer.SerializeToFile(config);
        }

        private void Light_Click(object sender, RoutedEventArgs e)
        {
            config.Theme = ThemeManager.Light;
            ThemeManager.Apply(config.Theme);
            ConfigSerializer.SerializeToFile(config);
        }

        private void btnStartSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStartSearch.IsEnabled = false;
                pBarSearchCount.IsIndeterminate = true;
                ObservableCollection<SearchEntry> tempSearch = new ObservableCollection<SearchEntry>();
                cTabItem = new CloseableTab.CloseableTabItem
                {
                    Header = tbxSearchContent.Text
                };
                tControlSearches.Items.Add(cTabItem);

                string searchString = tbxSearchContent.Text;

                if (searchString.Contains(" "))
                {
                    searchString = searchString.Replace(" ", "%20");
                }

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    webConnect.StartXMLFunction("/function/search?search=" + searchString + "&password=" + config.Password);
                }

                for (int i = 0; i < 2; i++)
                {
                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        appleJuice.Search = ((AppleJuice)appleJuice.DeserializeToObj(webConnect.GetHttpResult(getSearch, config.UseCompression))).Search;
                    }
                }
                Search _getSearch = appleJuice.Search.AsEnumerable().Where(a => a.Running.Equals(true)).First();
                searchInfo.FoundFiles = _getSearch.FoundFiles;
                searchInfo.Id = _getSearch.id;
                searchInfo.OpenSearches = _getSearch.OpenSearches;
                searchInfo.Running = _getSearch.Running;
                searchInfo.SumSearches = _getSearch.SumSearches;
                searchInfo.Users = appleJuice.NetworkInfo.Users;
                cTabItem.Tag = _getSearch.id;
                cTabItem.Focus();

                _stopSearch = false;
                _searchThread = new Thread(new ParameterizedThreadStart(_search_ThreadWorker))
                {
                    Name = "SearchWorker" + _getSearch.id,
                    IsBackground = true
                };
                _searchThread.Start(tempSearch);

                Binding bindImage = new Binding("FileName.Name")
                {
                    Converter = tControlSearches.Resources["FilenameToImage"] as IValueConverter
                };

                FrameworkElementFactory fefImage = new FrameworkElementFactory(typeof(Image));
                fefImage.SetBinding(Image.SourceProperty, bindImage);
                fefImage.SetValue(MarginProperty, new Thickness(0.0, 0.0, 5.0, 0.0));

                FrameworkElementFactory fefTextBlock = new FrameworkElementFactory(typeof(TextBlock));
                fefTextBlock.SetBinding(TextBlock.TextProperty, new Binding("FileName.Name"));

                FrameworkElementFactory fefStackPanel = new FrameworkElementFactory(typeof(StackPanel));
                fefStackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

                fefStackPanel.AppendChild(fefImage);
                fefStackPanel.AppendChild(fefTextBlock);

                DataTemplate dTemp = new DataTemplate
                {
                    VisualTree = fefStackPanel
                };

                Binding bind = new Binding("Size")
                {
                    Converter = (IValueConverter)tControlSearches.Resources["FileSizeConverter"]
                };

                Binding setterBind = new Binding("SearchColor")
                {
                    Converter = (IValueConverter)tControlSearches.Resources["SearchColor"]
                };

                Setter setter = new Setter(BackgroundProperty, setterBind);

                Style newStyle = new System.Windows.Style(typeof(DataGridRow))
                {
                    // auf den impliziten DataGridRow-Style aufsetzen, damit Hover/Auswahl weiter greifen
                    BasedOn = Application.Current.TryFindResource(typeof(DataGridRow)) as Style
                };
                newStyle.Setters.Add(setter);

                DataGrid dGrid = new DataGrid
                {
                    GridLinesVisibility = DataGridGridLinesVisibility.None,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    RowStyle = newStyle
                };

                DataGridTemplateColumn dColumnTemp = new DataGridTemplateColumn
                {
                    Header = Resources["filename_search"],
                    CellTemplate = dTemp,
                    CanUserSort = true,
                    SortMemberPath = "FileName.Name"
                };

                DataGridTextColumn dColumnSize = new DataGridTextColumn
                {
                    Header = Resources["filesize_search"],
                    Binding = bind
                };

                DataGridTextColumn dColumnsUser = new DataGridTextColumn
                {
                    Header = Resources["user_count"],
                    Binding = new Binding("FileName.User")
                };

                dGrid.Columns.Add(dColumnTemp);
                dGrid.Columns.Add(dColumnSize);
                dGrid.Columns.Add(dColumnsUser);
                tempSearch.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(tempSearch_CollectionChanged);
                dGrid.ItemsSource = tempSearch;
                cTabItem.Content = dGrid;
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Starten der Suche!", ex);
                btnStartSearch.IsEnabled = true;
                pBarSearchCount.IsIndeterminate = false;
            }
        }

        private void tempSearch_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Shares/Settings holt bereits _startSearch einmal pro Poll. Hier NUR die neu
            // hinzugekommenen Eintraege einfaerben - ohne HTTP und ohne Schleife ueber die
            // gesamte (waehrend der Suche wachsende) Ergebnisliste. Genau diese beiden Dinge
            // pro Add haben das Programm beim Klick ins DataGrid einfrieren lassen.
            if (e.NewItems == null || appleJuice.Shares?.Share == null || settings?.share?.Directory == null)
            {
                return;
            }

            foreach (SearchEntry newItem in e.NewItems.OfType<SearchEntry>())
            {
                IEnumerable<Share> getShares = appleJuice.Shares.Share.Where(a => a.CheckSum.Equals(newItem.Checksum));

                if (getShares.Count() > 0)
                {
                    foreach (Share share in getShares)
                    {
                        foreach (SettingsDirectory settingShare in settings.share.Directory)
                        {
                            if (share.FileName.Contains(settingShare.Name))
                            {
                                newItem.SearchColor = 1;
                            }
                            else
                            {
                                newItem.SearchColor = 2;
                            }
                        }
                    }
                }
                else
                {
                    newItem.SearchColor = 0;
                }
            }
        }

        private void btnStopSearch_Click(object sender, RoutedEventArgs e)
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                foreach (Search s in appleJuice.Search)
                {
                    webConnect.StartXMLFunction("/function/cancelsearch?id=" + s.id + "&password=" + config.Password);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void dGridDownloads_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dGridDownloads.SelectedItems.Count == 0)
            {
                mItemBreak.IsEnabled = false;
                mItemCancel.IsEnabled = false;
                mItemContinue.IsEnabled = false;
                mItemLink.IsEnabled = false;
                mItemRename.IsEnabled = false;
                mItemSource.IsEnabled = false;
                mItemTarget.IsEnabled = false;
            }
            else
            {
                switch ((dGridDownloads.SelectedItem as Download).Status)
                {
                    case 12:
                        mItemBreak.IsEnabled = false;
                        mItemCancel.IsEnabled = false;
                        mItemContinue.IsEnabled = false;
                        mItemLink.IsEnabled = true;
                        mItemRename.IsEnabled = false;
                        mItemSource.IsEnabled = true;
                        mItemTarget.IsEnabled = false;
                        break;
                    case 13:
                        mItemBreak.IsEnabled = false;
                        mItemCancel.IsEnabled = false;
                        mItemContinue.IsEnabled = false;
                        mItemLink.IsEnabled = true;
                        mItemRename.IsEnabled = false;
                        mItemSource.IsEnabled = true;
                        mItemTarget.IsEnabled = false;
                        break;
                    case 14:
                        mItemBreak.IsEnabled = false;
                        mItemCancel.IsEnabled = false;
                        mItemContinue.IsEnabled = false;
                        mItemLink.IsEnabled = true;
                        mItemRename.IsEnabled = false;
                        mItemSource.IsEnabled = true;
                        mItemTarget.IsEnabled = false;
                        break;
                    case 15:
                        mItemBreak.IsEnabled = false;
                        mItemCancel.IsEnabled = false;
                        mItemContinue.IsEnabled = false;
                        mItemLink.IsEnabled = true;
                        mItemRename.IsEnabled = false;
                        mItemSource.IsEnabled = true;
                        mItemTarget.IsEnabled = false;
                        break;
                    case 17:
                        mItemBreak.IsEnabled = false;
                        mItemCancel.IsEnabled = false;
                        mItemContinue.IsEnabled = false;
                        mItemLink.IsEnabled = true;
                        mItemRename.IsEnabled = false;
                        mItemSource.IsEnabled = true;
                        mItemTarget.IsEnabled = false;
                        break;
                    default:
                        mItemBreak.IsEnabled = true;
                        mItemCancel.IsEnabled = true;
                        mItemContinue.IsEnabled = true;
                        mItemLink.IsEnabled = true;
                        mItemRename.IsEnabled = true;
                        mItemSource.IsEnabled = true;
                        mItemTarget.IsEnabled = true;
                        break;
                }
            }
        }

        private void dGridShares_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dGridShares.SelectedItems.Count == 0)
            {
                mItemShareLink.IsEnabled = false;
                mitemShareSource.IsEnabled = false;
            }
            else
            {
                mItemShareLink.IsEnabled = true;
                mitemShareSource.IsEnabled = true;
            }
        }

        private void tControlSearches_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TabControl tCon = sender as TabControl;
                CloseableTab.CloseableTabItem tItem = tCon.SelectedItem as CloseableTab.CloseableTabItem;

                DataGrid dGrid = tItem.Content as DataGrid;


                if (dGrid.SelectedItems.Count == 0)
                {
                    mItemDownload.IsEnabled = false;
                    mItemSearchLink.IsEnabled = false;
                    mItemSearchSource.IsEnabled = false;
                }
                else
                {
                    mItemDownload.IsEnabled = true;
                    mItemSearchLink.IsEnabled = true;
                    mItemSearchSource.IsEnabled = true;
                }
            }
            catch
            {
                mItemDownload.IsEnabled = false;
                mItemSearchLink.IsEnabled = false;
                mItemSearchSource.IsEnabled = false;
            }
        }

        private void mItemCopyLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Server server = dGridServer.SelectedItem as Server;

                if (server == null)
                {
                    return;
                }

                string txt = string.Format("ajfsp://server|{0}|{1}/", server.Host, server.Port);
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void dGridServer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dGridServer.SelectedItems.Count == 0)
            {
                mItemConnectServer.IsEnabled = false;
                mItemDeleteServer.IsEnabled = false;
                mItemCopyLink.IsEnabled = false;
            }
            else
            {
                mItemConnectServer.IsEnabled = true;
                mItemDeleteServer.IsEnabled = true;
                mItemCopyLink.IsEnabled = true;
            }
        }

        private void rBtnActive_Checked(object sender, RoutedEventArgs e)
        {
            if (_refreshUserPartList != null)
            {
                if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshUserPartList = null;
                }
            }

            users.Clear();
        }

        private void rBtnWait_Checked(object sender, RoutedEventArgs e)
        {
            if (_refreshUserPartList != null)
            {
                if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshUserPartList = null;
                }
            }

            users.Clear();
        }

        private void rBtnRest_Checked(object sender, RoutedEventArgs e)
        {
            if (_refreshUserPartList != null)
            {
                if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshUserPartList = null;
                }
            }

            users.Clear();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Properties.Settings.Default.WindowHeight = Height;
            Properties.Settings.Default.WindowWidth = Width;

            Properties.Settings.Default.Save();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.WindowTop = Top;
            Properties.Settings.Default.WindowLeft = Left;

            Properties.Settings.Default.Save();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.WindowState = WindowState;
        }

        private void mItemEnableFolder_Click(object sender, RoutedEventArgs e)
        {
            if (tViewSharePaths.SelectedItem != null)
            {
                if (tViewSharePaths.SelectedItem.GetType() == typeof(DirectoryChildren))
                {
                    int countShare = 0;
                    string shareDirs = string.Empty, shareSubs = string.Empty;

                    DirectoryChildren dChild = tViewSharePaths.SelectedItem as DirectoryChildren;

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        settings = settings.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/settings.xml?password={0}&mode=zip", config.Password), config.UseCompression)) as Settings;
                    }

                    string[] dirs = new string[settings.share.Directory.Count + 1];
                    bool[] sharemode = new bool[settings.share.Directory.Count + 1];

                    for (int i = 0; i < settings.share.Directory.Count + 1; i++)
                    {
                        if (i == settings.share.Directory.Count)
                        {
                            shareDirs += string.Format("&sharedirectory{0}={1}", i + 1, dChild.Dir.Path.Replace(" ", "%20"));
                            shareSubs += string.Format("&sharesub{0}={1}", i + 1, true);
                            countShare += 1;
                            continue;
                        }
                        dirs[i] = settings.share.Directory[i].Name;
                        if (settings.share.Directory[i].ShareMode == "subdirectory")
                        {
                            sharemode[i] = true;
                        }
                        else
                        {
                            sharemode[i] = false;
                        }

                        countShare += 1;
                        shareDirs += string.Format("&sharedirectory{0}={1}", i + 1, dirs[i].Replace(" ", "%20"));
                        shareSubs += string.Format("&sharesub{0}={1}", i + 1, sharemode[i]);
                    }

                    string anfrage = string.Format("/function/setsettings?countshares={0}{1}{2}&password={3}", countShare, shareDirs, shareSubs, config.Password);

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(anfrage);
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        settings = settings.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/settings.xml?password={0}&mode=zip", config.Password), config.UseCompression)) as Settings;
                    }

                    sPanelShareFolders.Children.Clear();

                    foreach (SettingsDirectory sDir in settings.share.Directory)
                    {
                        StackPanel sPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };

                        CheckBox cBox = new CheckBox
                        {
                            IsChecked = true
                        };
                        cBox.Unchecked += new RoutedEventHandler(cBox_UnChecked);

                        TextBlock tBlock = new TextBlock
                        {
                            Text = sDir.Name,
                            Margin = new Thickness(5, 0, 0, 0)
                        };

                        sPanel.Children.Add(cBox);
                        sPanel.Children.Add(tBlock);

                        sPanelShareFolders.Children.Add(sPanel);
                    }
                }
            }
        }

        private void cBox_UnChecked(object sender, RoutedEventArgs e)
        {
            if (e.Source.GetType().Equals(typeof(CheckBox)))
            {
                CheckBox cBox = e.Source as CheckBox;
                StackPanel sPanel = cBox.Parent as StackPanel;
                TextBlock tBlock = sPanel.Children[sPanel.Children.Count - 1] as TextBlock;

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    settings = settings.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression)) as Settings;
                }

                int countShare = 0;
                string shareDirs = string.Empty, shareSubs = string.Empty;

                List<string> dirs = new List<string>();
                List<bool> sharemode = new List<bool>();

                IEnumerable<SettingsDirectory> getShares = settings.share.Directory.Where(a => a.Name != tBlock.Text);

                foreach (SettingsDirectory item in getShares)
                {

                    dirs.Add(item.Name);
                    if (settings.share.Directory[settings.share.Directory.IndexOf(item)].ShareMode == "subdirectory")
                    {
                        sharemode.Add(true);
                    }
                    else
                    {
                        sharemode.Add(false);
                    }

                    countShare += 1;
                    shareDirs += string.Format("&sharedirectory{0}={1}", countShare, dirs[dirs.IndexOf(item.Name)].Replace(" ", "%20"));
                    shareSubs += string.Format("&sharesub{0}={1}", countShare, sharemode[countShare - 1]);
                }

                string anfrage = string.Format("/function/setsettings?countshares={0}{1}{2}&password={3}", countShare, shareDirs, shareSubs, config.Password);

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    webConnect.StartXMLFunction(anfrage);
                }

                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    settings = settings.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression)) as Settings;
                }

                sPanelShareFolders.Children.Clear();

                foreach (SettingsDirectory sDir in settings.share.Directory)
                {
                    StackPanel spanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal
                    };

                    CheckBox cbox = new CheckBox
                    {
                        IsChecked = true
                    };
                    cbox.Unchecked += new RoutedEventHandler(cBox_UnChecked);

                    TextBlock tblock = new TextBlock
                    {
                        Text = sDir.Name,
                        Margin = new Thickness(5, 0, 0, 0)
                    };

                    spanel.Children.Add(cbox);
                    spanel.Children.Add(tblock);

                    sPanelShareFolders.Children.Add(spanel);
                }
            }
        }

        private void mItemEnableFolder_Click_2(object sender, RoutedEventArgs e)
        {
            if (tViewSharePaths.SelectedItem != null)
            {
                if (tViewSharePaths.SelectedItem.GetType() == typeof(DirectoryChildren))
                {
                    int countShare = 0;
                    string shareDirs = string.Empty, shareSubs = string.Empty;

                    DirectoryChildren dChild = tViewSharePaths.SelectedItem as DirectoryChildren;

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        settings = settings.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/settings.xml?password={0}&mode=zip", config.Password), config.UseCompression)) as Settings;
                    }

                    string[] dirs = new string[settings.share.Directory.Count + 1];
                    bool[] sharemode = new bool[settings.share.Directory.Count + 1];

                    for (int i = 0; i < settings.share.Directory.Count + 1; i++)
                    {
                        if (i == settings.share.Directory.Count)
                        {
                            shareDirs += string.Format("&sharedirectory{0}={1}", i + 1, dChild.Dir.Path.Replace(" ", "%20"));
                            shareSubs += string.Format("&sharesub{0}={1}", i + 1, false);
                            countShare += 1;
                            continue;
                        }
                        dirs[i] = settings.share.Directory[i].Name;
                        if (settings.share.Directory[i].ShareMode == "subdirectory")
                        {
                            sharemode[i] = true;
                        }
                        else
                        {
                            sharemode[i] = false;
                        }

                        countShare += 1;
                        shareDirs += string.Format("&sharedirectory{0}={1}", i + 1, dirs[i].Replace(" ", "%20"));
                        shareSubs += string.Format("&sharesub{0}={1}", i + 1, sharemode[i]);
                    }

                    string anfrage = string.Format("/function/setsettings?countshares={0}{1}{2}&password={3}", countShare, shareDirs, shareSubs, config.Password);

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        webConnect.StartXMLFunction(anfrage);
                    }

                    using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                    {
                        settings = settings.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/settings.xml?password={0}&mode=zip", config.Password), config.UseCompression)) as Settings;
                    }

                    sPanelShareFolders.Children.Clear();

                    foreach (SettingsDirectory sDir in settings.share.Directory)
                    {
                        StackPanel sPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };

                        CheckBox cBox = new CheckBox
                        {
                            IsChecked = true
                        };
                        cBox.Unchecked += new RoutedEventHandler(cBox_UnChecked);

                        TextBlock tBlock = new TextBlock
                        {
                            Text = sDir.Name,
                            Margin = new Thickness(5, 0, 0, 0)
                        };

                        sPanel.Children.Add(cBox);
                        sPanel.Children.Add(tBlock);

                        sPanelShareFolders.Children.Add(sPanel);
                    }
                }
            }
        }

        private void cBoxPartList_Checked(object sender, RoutedEventArgs e)
        {
            if (dGridDownloads.SelectedItems.Count == 1)
            {
                _refreshDownloadPartList = new Thread(new ThreadStart(_refreshDownloadPartList_ThreadWorker));
                _refreshDownloadPartList.Name += "RefreshDownloadPartList";
                _refreshDownloadPartList.IsBackground = true;
                _refreshDownloadPartList.Start();
            }
        }

        private void cBoxPartList_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_refreshDownloadPartList != null)
            {
                if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshDownloadPartList = null;
                }
            }

            if (_refreshUserPartList != null)
            {
                if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshUserPartList = null;
                }
            }

            imgPartList.Source = null;
        }

        private void dGridDownloadInfos_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (_refreshDownloadPartList != null)
            {
                if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshDownloadPartList = null;
                }
            }

            if (_refreshUserPartList != null)
            {
                if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                {
                    _refreshUserPartList = null;
                }
            }

            if (cBoxPartList.IsChecked == true)
            {
                selectionUserGrid = sender as DataGrid;
                selectedUser = selectionUserGrid.SelectedItem as User;

                _refreshUserPartList = new Thread(new ThreadStart(_refreshUserPartList_ThreadWorker))
                {
                    Name = "UpdateUsers",
                    IsBackground = true
                };
                _refreshUserPartList.Start();
            }
        }

        private void dGridDownloads_GotFocus(object sender, RoutedEventArgs e)
        {
            Download dLoad = dGridDownloads.SelectedItem as Download;

            if (cBoxPartList.IsChecked == true)
            {
                if (dLoad != null)
                {
                    {
                        if (_refreshDownloadPartList != null)
                        {
                            if ((_refreshDownloadPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                            {
                                _refreshDownloadPartList = null;
                            }
                        }

                        if (_refreshUserPartList != null)
                        {
                            if ((_refreshUserPartList.ThreadState & ThreadState.Running) == ThreadState.Running)
                            {
                                _refreshUserPartList = null;
                            }
                        }

                        _refreshDownloadPartList = new Thread(new ThreadStart(_refreshDownloadPartList_ThreadWorker));
                        _refreshDownloadPartList.Name += "RefreshDownloadPartList";
                        _refreshDownloadPartList.IsBackground = true;
                        _refreshDownloadPartList.Start();
                    }
                }
            }
        }
        #endregion

        private void DGridDownloads_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    Continue_Click(sender, e);
                    break;
                case Key.F3:
                    Break_Click(sender, e);
                    break;
                case Key.F4:
                    Cancel_Click(sender, e);
                    break;
                case Key.F6:
                    Rename_Click(sender, e);
                    break;
                case Key.F7:
                    TargetFolder_Click(sender, e);
                    break;
                case Key.F5:
                    Finished_Click(sender, e);
                    break;
                case Key.F8:
                    Link_Click(sender, e);
                    break;
                case Key.F9:
                    Source_Click(sender, e);
                    break;
                default:
                    break;
            }
        }

        private void DGridShares_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F8:
                    ShareLink_Click(sender, e);
                    break;
                case Key.F9:
                    ShareSource_Click(sender, e);
                    break;
                case Key.F10:
                    ShareLinks_Click(sender, e);
                    break;
                case Key.F11:
                    ShareSources_Click(sender, e);
                    break;
                default:
                    SelectDataGridRow(sender, e);
                    break;
            }
        }
    }
}
