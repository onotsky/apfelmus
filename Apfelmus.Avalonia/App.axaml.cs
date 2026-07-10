using System;
using System.Linq;
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
        private MainWindowViewModel? _mainVm;
        private string? _pendingLink;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // macOS/URL-Scheme: eingehende ajfsp://-Links kommen als Aktivierung (Apple-Event),
            // nicht als Kommandozeilen-Argument.
            if (this.TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatable)
            {
                activatable.Activated += OnActivated;
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

                // ajfsp-Link aus der Kommandozeile (Windows/Linux-Protokoll-Handler).
                string? argvLink = desktop.Args?.FirstOrDefault(
                    a => a.StartsWith("ajfsp://", StringComparison.OrdinalIgnoreCase));

                // Zeigt den Splashscreen und oeffnet danach das Hauptfenster (Splash kommt NACH dem Login).
                void ShowSplashThenMain(ApfelmusFramework.Classes.Config.Config config)
                {
                    var splash = new SplashWindow();
                    splash.Show();
                    global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
                    {
                        var vm = new MainWindowViewModel(config, _pendingLink ?? argvLink);
                        _pendingLink = null;
                        _mainVm = vm;
                        var main = new MainWindow { DataContext = vm };
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
                    ShowSplashThenMain(saved!);
                }
                else
                {
                    var loginVm = new LoginViewModel();
                    var login = new LoginWindow { DataContext = loginVm };
                    loginVm.LoginSucceeded += config =>
                    {
                        ShowSplashThenMain(config);
                        login.Close();
                    };
                    desktop.MainWindow = login; // wird beim Start automatisch angezeigt
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnActivated(object? sender, ActivatedEventArgs e)
        {
            if (e is ProtocolActivatedEventArgs p && p.Kind == ActivationKind.OpenUri && p.Uri != null)
            {
                string link = p.Uri.ToString();
                if (_mainVm != null)
                {
                    _mainVm.ProcessExternalLink(link);
                }
                else
                {
                    // Aktivierung vor dem Erstellen des Hauptfensters -> merken und beim Start verarbeiten.
                    _pendingLink = link;
                }
            }
        }
    }
}
