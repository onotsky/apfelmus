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
        // Mehrere Links koennen vor dem Hauptfenster eintreffen (z.B. Kaltstart mit mehreren
        // ajfsp-Links) - daher SAMMELN statt ueberschreiben.
        private readonly System.Collections.Generic.List<string> _pendingLinks = new();

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

            // macOS: Avalonia 11.2 loest das OpenUri-Event nicht aus - deshalb faengt ein nativer
            // NSAppleEventManager-Handler den ajfsp-Link direkt ueber das Apple-Event ab.
            // WICHTIG: erst NACH Cocoas eigener finishLaunching-Registrierung setzen (sonst wird unser
            // Handler ueberschrieben). Fuer den Kaltstart moeglichst FRUEH greifen -> mehrfach
            // (idempotent) registrieren: sofort per Post und dann gestaffelt.
            // Windows/Linux: von weiteren (Single-Instance-)Instanzen weitergereichte Links verarbeiten.
            if (!OperatingSystem.IsMacOS())
            {
                Services.SingleInstance.SetHandler(url =>
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() => HandleIncomingLink(url)));
            }

            if (OperatingSystem.IsMacOS())
            {
                void RegisterMac()
                {
                    if (OperatingSystem.IsMacOS())
                        Services.MacUrlScheme.Register(url =>
                            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => HandleIncomingLink(url)));
                }
                global::Avalonia.Threading.Dispatcher.UIThread.Post(RegisterMac);
                foreach (int ms in new[] { 200, 600, 1200 })
                    global::Avalonia.Threading.DispatcherTimer.RunOnce(RegisterMac, TimeSpan.FromMilliseconds(ms));
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
                        var vm = new MainWindowViewModel(config);
                        _mainVm = vm;
                        var main = new MainWindow { DataContext = vm };
                        main.Show();
                        splash.Close();

                        // Alle bis hierher gesammelten Links verarbeiten (Kommandozeile + gesammelte Aktivierungen).
                        if (!string.IsNullOrWhiteSpace(argvLink)) vm.ProcessExternalLink(argvLink!);
                        foreach (var l in _pendingLinks) vm.ProcessExternalLink(l);
                        _pendingLinks.Clear();
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
                HandleIncomingLink(p.Uri.ToString());
            }
        }

        /// <summary>Verarbeitet einen eingehenden ajfsp-Link (aus OpenUri ODER dem nativen macOS-Handler).</summary>
        private void HandleIncomingLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link)) return;
            if (_mainVm != null)
                _mainVm.ProcessExternalLink(link!);
            else
                _pendingLinks.Add(link!); // vor dem Hauptfenster -> beim Start alle verarbeiten
        }
    }
}
