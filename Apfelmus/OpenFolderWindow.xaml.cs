using System.Collections.Generic;
using System.Windows;
using ApfelmusFramework.Classes.Allgemein;
using ApfelmusFramework.Classes.Config;
using ApfelmusFramework.Classes.Directory;
using ApfelmusFramework.Classes.Help;
using ApfelmusFramework.Classes.Logic;
using ApfelmusFramework.Classes.Settings;

namespace Apfelmus
{
    /// <summary>
    /// Interaktionslogik für OpenFolderWindow.xaml
    /// </summary>
    public partial class OpenFolderWindow : Window
    {
        Config config;
        AppleJuice dir = new AppleJuice();
        AppleJuice infos = new AppleJuice();
        Settings settings;

        string getDir, getInfos;
        List<DirectoryChildren> item = new List<DirectoryChildren>();

        private string selectedPath;

        public string SelectedPath
        {
            get { return selectedPath; }
            set { selectedPath = value; }
        }

        public OpenFolderWindow(ResourceDictionary rDict, Config config, Settings settings)
        {
            InitializeComponent();

            this.Resources.MergedDictionaries.Add(rDict);
            this.config = config;
            getInfos = string.Format("/xml/information.xml?password={0}&mode=zip", config.Password);
            getDir = string.Format("/xml/directory.xml?password={0}&mode=zip", config.Password);
            this.settings = settings;
        }

        private void btnFolderBrowserOk_Click_1(object sender, RoutedEventArgs e)
        {
            if (tViewFolderBrowser.SelectedItem != null)
            {
                if (tViewFolderBrowser.SelectedItem.GetType().Equals(typeof(DirectoryChildren)))
                {
                    DirectoryChildren dChild = tViewFolderBrowser.SelectedItem as DirectoryChildren;

                    selectedPath = dChild.Dir.Path;
                    this.DialogResult = true;
                    this.Close();
                }
            }

        }

        private void btnFolderBrowserCancel_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OpenFolderWindow_Loaded_1(object sender, RoutedEventArgs e)
        {
            GenerateFirstTreeItem();
            tViewFolderBrowser.ItemsSource = item;
        }

        private void GenerateFirstTreeItem()
        {
            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                dir = dir.DeserializeToObj(webConnect.GetHttpResult(string.Format("/xml/directory.xml?password={0}&mode=zip", config.Password), config.UseCompression)) as AppleJuice;
            }

            List<Dir> dirList = new List<Dir>();
            foreach (Dir _dir in dir.Dir)
            {
                if (string.IsNullOrEmpty(_dir.Path))
                {
                    if (dir.FileSystem.Seperator.Equals("/"))
                        _dir.Path = _dir.Name;
                    else
                        _dir.Path = string.Format("{0}{1}", dir.FileSystem.Seperator, _dir.Name);
                }
                item.Add(new DirectoryChildren(_dir, config));

                foreach (Dir __dir in _dir.Directory)
                {
                    if (string.IsNullOrEmpty(__dir.Path))
                    {
                        if (dir.FileSystem.Seperator.Equals("/"))
                            __dir.Path = string.Format("{0}{1}{2}", _dir.Path, __dir.Name, dir.FileSystem.Seperator);
                        else
                            __dir.Path = string.Format("{0}{1}{2}", _dir.Path, dir.FileSystem.Seperator, __dir.Name);
                    }

                    item.Add(new DirectoryChildren(__dir, config));
                }
            }
        }

    }
}
