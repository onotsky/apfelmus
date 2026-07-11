using System;
using System.Linq;
using Avalonia;

namespace Apfelmus.Avalonia
{
    internal static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            string? link = args.FirstOrDefault(a => a.StartsWith("ajfsp://", StringComparison.OrdinalIgnoreCase));

            // Single-Instance (Windows/Linux): eingehende ajfsp-Links starten sonst je EINE neue
            // Instanz. macOS ist ueber das .app-Bundle ohnehin Single-Instance.
            if (!OperatingSystem.IsMacOS())
            {
                if (Services.SingleInstance.TryBecomePrimary())
                {
                    Services.SingleInstance.StartServer();
                }
                else
                {
                    Services.SingleInstance.ForwardToRunning(link); // an laufende Instanz uebergeben
                    return;                                          // und beenden
                }
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
