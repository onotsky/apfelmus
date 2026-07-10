using Avalonia;
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

                var loginVm = new LoginViewModel();
                var login = new LoginWindow { DataContext = loginVm };

                loginVm.LoginSucceeded += config =>
                {
                    var main = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(config)
                    };
                    main.Show();
                    login.Close();
                };

                desktop.MainWindow = login;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
