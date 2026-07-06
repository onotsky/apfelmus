using System;
using System.Windows;
using ApfelmusFramework.Classes.Logic;

namespace Apfelmus
{
    /// <summary>
    /// Anwendungseinstieg. Setzt beim Start das gespeicherte Theme, richtet die Single-Instance-
    /// Erkennung ein (weitere Starts - z.B. aus einem ajfsp://-Link - reichen ihre Argumente per
    /// Named Pipe an die laufende Instanz weiter) und oeffnet das Hauptfenster.
    /// </summary>
    public partial class App : Application
    {
        private readonly Guid _appGuid = new Guid("{C307A02B-6F41-4996-B330-97045F4A07BC}");

        public static string[] _args;

        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.ApplyStartupTheme();

            SingleInstance si = new SingleInstance(_appGuid);
            si.ArgsRecieved += new SingleInstance.ArgsHandler(si_ArgsReceived);
            si.Run(() =>
            {
                new MainWindow(e.Args).Show();
                return this.MainWindow;
            }, e.Args);
        }

        public void si_ArgsReceived(string[] args)
        {
            if (args.Length > 0)
            {
                Apfelmus.MainWindow.args.Add(args[0]);
            }
        }
    }
}
