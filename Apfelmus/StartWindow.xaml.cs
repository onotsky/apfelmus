//-----------------------------------------------------------------------
// <copyright file="AppleJuice.xaml.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------

using System;
using System.Windows;
using ApfelmusFramework.Classes.Config;
using ApfelmusFramework.Classes.Serializer;
using ApfelmusFramework.Classes.Logic;
using System.Windows.Media;
using System.Windows.Input;

namespace Apfelmus
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class BaseWindow : Window
    {
        private MainWindow mainWindow;
        public static Config config;

        public BaseWindow(MainWindow mainWindow, ResourceDictionary dict)
        {
            InitializeComponent();

            this.Resources.MergedDictionaries.Add(dict);
                        
            this.mainWindow = mainWindow;
            tbxHost.DataContext = config;
            tbxPort.DataContext = config;
            chbxSetPass.DataContext = config;
            chbxUseCompress.DataContext = config;
            pbxPasswd.KeyDown += new KeyEventHandler(pbxPasswd_KeyDown);
        }

        private void pbxPasswd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Return) || e.Key.Equals(Key.Enter))
                btnOk_Click(this, new RoutedEventArgs());
        }

        public static void CheckConfig()
        {
            try
            {
                config = BinarySerializer.DeserializeFromFile();
                if (config.RefreshRate.Equals(0))
                    config.RefreshRate = 1500;
            }
            catch
            {
                config = new Config();
                config.HideLoginWindow = false;
                config.HostName = "127.0.0.1";
                config.Password = string.Empty;
                config.Port = 9851;
                config.UseCompression = false;
                config.RefreshRate = 1500;
                config.LanguageFile = LanguageDictionary.GetURI();
                BinarySerializer.SerializeToFile(config);
            }
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(10);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (pbxPasswd.Password.Length != 0 || tbxHost.Text == "localhost" || tbxHost.Text == "127.0.0.1")
            {
                using (WebConnect webConnect = new WebConnect(config.HostName, config.Port))
                {
                    if (webConnect.CheckSocket(tbxHost.Text, Convert.ToInt32(tbxPort.Text)))
                    {
                        config.Password = CreateMd5Hash.GetMD5Hash(pbxPasswd.Password);
                        MainWindow.config = config;
                        BinarySerializer.SerializeToFile(config);
                        this.Close();
                    }
                    else
                    {
                        tbxHost.BorderThickness = new Thickness(2.0, 2.0, 2.0, 2.0);
                        tbxHost.BorderBrush = Brushes.Red;
                        pbxPasswd.Password = string.Empty;
                    }
                }
            }
        }

        private void pbxPasswd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (CreateMd5Hash.GetMD5Hash(pbxPasswd.Password) == config.Password)
            {
                pbxPasswd.BorderThickness = new Thickness(2.0, 2.0, 2.0, 2.0);
                pbxPasswd.BorderBrush = Brushes.LightGreen;
            }
            else
            {

                if (pbxPasswd.Password.Equals(string.Empty))
                {
                    pbxPasswd.BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0);
                    pbxPasswd.BorderBrush = tbxPort.BorderBrush;
                }
                else
                {
                    pbxPasswd.BorderThickness = new Thickness(2.0, 2.0, 2.0, 2.0);
                    pbxPasswd.BorderBrush = Brushes.Red;
                }
            }
        }
    }
}
