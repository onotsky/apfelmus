using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Apfelmus.Avalonia.ViewModels;
using Apfelmus.Avalonia.Views;

namespace Apfelmus.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Beim Schliessen des zuletzt offenen Fensters beenden - so ueberlebt die App
                // den Wechsel vom Login- zum Hauptfenster.
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

                // Zeigt den Splashscreen und oeffnet danach das Hauptfenster (Splash kommt NACH dem Login).
                void ShowSplashThenMain(ApfelmusFramework.Classes.Config.Config config)
                {
                    var splash = new SplashWindow();
                    splash.Show();
                    global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
                    {
                        var main = new MainWindow { DataContext = new MainWindowViewModel(config) };
                        main.Show();
                        splash.Close();
                    }, System.TimeSpan.FromSeconds(1.6));
                }

                // Gespeicherte Config laden - bei "Login ausblenden" + vorhandenem Passwort direkt starten.
                ApfelmusFramework.Classes.Config.Config? saved = null;
                try { saved = ApfelmusFramework.Classes.Serializer.ConfigSerializer.DeserializeFromFile(); }
                catch { /* keine/erste Config */ }
                bool skipLogin = saved != null && saved.HideLoginWindow && !string.IsNullOrEmpty(saved.Password);

                if (skipLogin)
                {
                    // Kein Login-Fenster: direkt Splash -> Hauptfenster.
                    ShowSplashThenMain(saved!);
                }
                else
                {
                    var loginVm = new LoginViewModel();
                    var login = new LoginWindow { DataContext = loginVm };
                    loginVm.LoginSucceeded += config =>
                    {
                        // Erst Splash zeigen, dann Login schliessen (immer >= 1 Fenster offen).
                        ShowSplashThenMain(config);
                        login.Close();
                    };
                    desktop.MainWindow = login; // wird beim Start automatisch angezeigt
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
