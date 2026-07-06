using System.Windows;
using ApfelmusFramework.Classes.Config;
using ApfelmusFramework.Classes.Modified;
using ApfelmusFramework.Classes.Logic;

namespace Apfelmus
{
    /// <summary>
    /// Dialog zum Umbenennen eines Downloads. Erlaubt zusaetzlich das Ersetzen bzw. Loeschen einer
    /// Teilzeichenkette im Namen (jeweils ohne die Endung anzutasten) und schickt den neuen Namen
    /// per /function/renamedownload an den Core.
    /// </summary>
    public partial class RenameDownloadWindow : Window
    {
        private Config config;
        private Download dLoad;

        public RenameDownloadWindow(Config config, Download dLoad, ResourceDictionary rDict)
        {
            InitializeComponent();
            this.config = config;
            this.dLoad = dLoad;
            this.Resources = rDict;
            tBoxNewFileName.Text = dLoad.FileName;
            tBoxNewFileName.Select(0, tBoxNewFileName.Text.LastIndexOf('.'));
            tBoxNewFileName.Focus();
        }

        public void btnRename_Click(object sender, RoutedEventArgs e)
        {
            if (cBoxReplaceString.IsChecked == true)
                tBoxNewFileName.Text = tBoxNewFileName.Text.Substring(0,tBoxNewFileName.Text.LastIndexOf('.')).Replace(tBoxOldString.Text, tBoxNewString.Text) + tBoxNewFileName.Text.Substring(tBoxNewFileName.Text.LastIndexOf('.'));
            
            if (cBoxDeleteString.IsChecked == true)
                tBoxNewFileName.Text = tBoxNewFileName.Text.Substring(0, tBoxNewFileName.Text.LastIndexOf('.')).Replace(tBoxDeleteString.Text, string.Empty) + tBoxNewFileName.Text.Substring(tBoxNewFileName.Text.LastIndexOf('.'));

            string tmpStr = string.Format("/function/renamedownload?id={0}&name={1}&password={2}", new object[] { dLoad.Id, tBoxNewFileName.Text, config.Password });

            using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
            {
                webConnect.StartXMLFunction(tmpStr);
            }
            this.Close();
        }

        private void btnRenameCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cBoxReplaceString_Checked(object sender, RoutedEventArgs e)
        {
            tBoxOldString.IsEnabled = true;
            tBoxNewString.IsEnabled = true;
        }

        private void cBoxReplaceString_Unchecked(object sender, RoutedEventArgs e)
        {
            tBoxOldString.IsEnabled = false;
            tBoxNewString.IsEnabled = false;
        }

        private void cBoxDeleteString_Checked(object sender, RoutedEventArgs e)
        {
            tBoxDeleteString.IsEnabled = true;
        }

        private void cBoxDeleteString_Unchecked(object sender, RoutedEventArgs e)
        {
            tBoxDeleteString.IsEnabled = false;
        }
    }
}
