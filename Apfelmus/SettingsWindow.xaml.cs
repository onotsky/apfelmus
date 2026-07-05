using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Media;
using ApfelmusFramework.Classes.Config;
using ApfelmusFramework.Classes.Settings;
using ApfelmusFramework.Classes.Logic;
using ApfelmusFramework.Classes.Serializer;
using Microsoft.Win32;
using System.Security.Principal;
using log4net;

namespace Apfelmus
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private static Settings settings = new Settings();
        private Settings newSettings = new Settings();

        private ILog logger;
        private Config config;
        private List<KeyValuePair<int, int>> refreshRateValues = new List<KeyValuePair<int, int>>();

        public List<KeyValuePair<int, int>> RefreshRateValues
        {
            get { return refreshRateValues; }
            set { refreshRateValues = value; }
        }

        private int refreshRateValue;

        public int RefreshRateValue
        {
            get { return refreshRateValue; }
            set
            {
                refreshRateValue = value;
                OnPropertyChanged("RefreshRateValue");
            }
        }

        public SettingsWindow(Config config, ResourceDictionary dict)
        {

            InitializeComponent();

            log4net.Config.XmlConfigurator.Configure();
            logger = LogManager.GetLogger(typeof(SettingsWindow));

            this.Resources.MergedDictionaries.Add(dict);
            this.config = config;

            for (int i = 1000; i <= 4000; i += 100)
                RefreshRateValues.Add(new KeyValuePair<int, int>(i, i));

            try
            {
                settings = GetSettings();
                SetBindings();
                if (config.RefreshRate.Equals(0))
                    RefreshRateValue = 1500;
                else
                    RefreshRateValue = config.RefreshRate;
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim setzen der Bindings (Settings)", ex);
            }
        }

        private void btnChangeSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetSettings();
                CreateProtocolHandler();
                if (cBoxShowStartWindow.IsChecked == true)
                    ShowLoginWindow();
                if (pBoxPassword.Password.Length > 0)
                    ChangePassword();
                if (config.RefreshRate != RefreshRateValue)
                    ChangeRefreshRate();
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Schreiben der Änderung (Settings)!", ex);
            }
        }

        private Settings GetSettings()
        {
            string getSettings = string.Format("/xml/settings.xml?password={0}&mode=zip", config.Password);
            Settings set = new Settings();
            using (WebConnect webConnect = new WebConnect(this.config.HostName, this.config.Port))
            {
                return (Settings)set.DeserializeToObj(webConnect.GetHttpResult(getSettings, config.UseCompression));
            }
        }

        private void SetSettings()
        {
            string setSettings = "/function/setsettings?";
            newSettings = GetSettings();

            if (settings.IncomingDirectory != newSettings.IncomingDirectory)
                setSettings += "Incomingdirectory=" + settings.IncomingDirectory + "&";
            if (settings.MaxConnections != newSettings.MaxConnections)
                setSettings += "MaxConnections=" + settings.MaxConnections + "&";
            if (settings.MaxDownload != newSettings.MaxDownload)
                setSettings += "MaxDownload=" + settings.MaxDownload + "&";
            if (settings.MaxNewConnectionsPerTurn != newSettings.MaxNewConnectionsPerTurn)
                setSettings += "MaxNewConnectionsPerTurn=" + settings.MaxNewConnectionsPerTurn + "&";
            if (settings.MaxSourcesPerFile != newSettings.MaxSourcesPerFile)
                setSettings += "MaxSourcesPerFile=" + settings.MaxSourcesPerFile + "&";
            if (settings.MaxUpload != newSettings.MaxUpload)
                setSettings += "MaxUpload=" + settings.MaxUpload + "&";
            if (settings.Nick != newSettings.Nick)
                setSettings += "Nickname=" + settings.Nick + "&";
            if (settings.Port != newSettings.Port)
                setSettings += "Port=" + settings.Port + "&";
            if (settings.SpeedPerSlot != newSettings.SpeedPerSlot)
                setSettings += "Speedperslot=" + settings.SpeedPerSlot + "&";
            if (settings.TemporaryDirectory != newSettings.TemporaryDirectory)
                setSettings += "Temporarydirectory=" + settings.TemporaryDirectory + "&";
            if (settings.XmlPort != newSettings.XmlPort)
                setSettings += "XMLPort=" + settings.XmlPort + "&";

            setSettings += "password=" + config.Password;


            using (WebConnect webConnect = new WebConnect(this.config.HostName, this.config.Port))
            {
                webConnect.GetHttpResult(setSettings, config.UseCompression);
            }
        }

        private void SetBindings()
        {
            tBoxCorePort.DataContext = settings;
            tBoxIncomingFolder.DataContext = settings;
            tBoxNickName.DataContext = settings;
            tBoxTempFolder.DataContext = settings;
            tBoxXmlPort.DataContext = settings;
            tBoxMaxConnections.DataContext = settings;
            tBoxMaxDownloadSpeed.DataContext = settings;
            tBoxMaxSources.DataContext = settings;
            tBoxMaxUploadSpeed.DataContext = settings;
            tBoxMaxNewConnectionsPerTurn.DataContext = settings;
            sliderUploadSlot.DataContext = settings;
            cbxAutoConnect.DataContext = settings;
            tBoxHostName.DataContext = config;
            cbxSetLink.DataContext = config;
            DataContext = this;
        }

        private void btnCancelChangeSettings_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ChangePassword()
        {
            try
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    webConnect.GetHttpResult("/function/setpassword?newpassword=" + CreateMd5Hash.GetMD5Hash(pBoxPassword.Password) + "&password=" + config.Password + "&mode=zip", config.UseCompression);
                }

                config.Password = CreateMd5Hash.GetMD5Hash(pBoxPassword.Password);
                BinarySerializer.SerializeToFile(config);

                pBoxPassword.Password = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Ändern des Passwortes!", ex);
            }
        }

        private void ShowLoginWindow()
        {
            try
            {
                config.HideLoginWindow = false;
                BinarySerializer.SerializeToFile(config);

            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Anzeigen des Login-Fensters!", ex);
            }
        }

        private void CreateProtocolHandler()
        {
            try
            {
                string oldValue = string.Empty;

                string newValue = "\"" + Environment.CurrentDirectory + "\\Apfelmus.exe\"" + " \"%1\"";
                try
                {
                    oldValue = Registry.ClassesRoot.OpenSubKey(@"ajfsp\shell\open\command").GetValue(string.Empty).ToString();
                }
                catch
                { }

                if (config.ProtocolHandler && oldValue != newValue)
                {
                    string user = Environment.UserDomainName + @"\" + Environment.UserName;

                    RegistrySecurity rs = new RegistrySecurity();

                    rs.AddAccessRule(new RegistryAccessRule(user, RegistryRights.FullControl, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));

                    using (RegistryKey rKey = Registry.ClassesRoot.CreateSubKey("ajfsp", RegistryKeyPermissionCheck.Default, rs))
                    {
                        rKey.SetValue(string.Empty, "URL: ajfsp Protocol");
                        rKey.SetValue("URL Protocol", string.Empty);
                    }

                    using (RegistryKey rKey = Registry.ClassesRoot.CreateSubKey(@"ajfsp\shell\open\command", RegistryKeyPermissionCheck.Default, rs))
                    {
                        rKey.SetValue(string.Empty, newValue);
                    }

                    config.ProtocolHandler = true;
                    BinarySerializer.SerializeToFile(config);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Erstellen des Protokollhandlers!", ex);
            }
        }

        private void btnChangeSettingsAndClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetSettings();
                CreateProtocolHandler();
                if (cBoxShowStartWindow.IsChecked == true)
                    ShowLoginWindow();
                if (pBoxPassword.Password.Length > 0)
                    ChangePassword();
                if (config.RefreshRate != RefreshRateValue)
                    ChangeRefreshRate();
            }
            catch (Exception ex)
            {
                logger.Error("Fehler beim Ändern der Settings!", ex);
            }
            finally
            {
                this.Close();
            }
        }

        private void ChangeRefreshRate()
        {
            config.RefreshRate = RefreshRateValue;
            BinarySerializer.SerializeToFile(config);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void TabItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                lblError.Visibility = System.Windows.Visibility.Visible;
                lblError.Foreground = Brushes.Red;
                cbxSetLink.IsEnabled = false;
            }
        }

        private void TabItem_LostFocus(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = System.Windows.Visibility.Hidden;
        }

        private void btnSelectTempPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderWindow oFolderWin = new OpenFolderWindow(this.Resources, config, settings);
            oFolderWin.Owner = this;
            oFolderWin.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            if (oFolderWin.ShowDialog() == true)
            {
                tBoxTempFolder.Text = oFolderWin.SelectedPath;
            }
        }

        private void btnSelectIncomingPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderWindow oFolderWin = new OpenFolderWindow(this.Resources, config, settings);
            oFolderWin.Owner = this;
            oFolderWin.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            if (oFolderWin.ShowDialog() == true)
            {
                tBoxIncomingFolder.Text = oFolderWin.SelectedPath;
            }
        }
    }
}
