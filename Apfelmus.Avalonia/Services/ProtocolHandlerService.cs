using System;
using System.Runtime.Versioning;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Registriert das ajfsp://-Protokoll unter Windows fuer diese Anwendung, damit Browser
    /// (z.B. Chrome) ajfsp-Links an Apfelmus uebergeben. Bewusst unter HKCU\Software\Classes
    /// (pro Benutzer, keine Adminrechte noetig). Auf Nicht-Windows-Systemen ein No-Op.
    /// </summary>
    public static class ProtocolHandlerService
    {
        public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

        public static bool Register()
        {
            if (OperatingSystem.IsWindows()) return RegisterWindows();
            if (OperatingSystem.IsLinux()) return RegisterLinux();
            return false;
        }

        [SupportedOSPlatform("linux")]
        private static bool RegisterLinux()
        {
            try
            {
                string exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                string appsDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "applications");
                System.IO.Directory.CreateDirectory(appsDir);
                const string desktopName = "apfelmus-ajfsp.desktop";
                string desktopPath = System.IO.Path.Combine(appsDir, desktopName);

                // .desktop mit URL-Scheme-Handler; %u uebergibt den ajfsp-Link als Argument.
                string content =
                    "[Desktop Entry]\n" +
                    "Type=Application\n" +
                    "Name=Apfelmus\n" +
                    "Exec=\"" + exe + "\" %u\n" +
                    "Terminal=false\n" +
                    "NoDisplay=true\n" +
                    "MimeType=x-scheme-handler/ajfsp;\n";
                System.IO.File.WriteAllText(desktopPath, content);

                Run("xdg-mime", "default " + desktopName + " x-scheme-handler/ajfsp");
                Run("update-desktop-database", appsDir);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [SupportedOSPlatform("linux")]
        private static void Run(string file, string args)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(file, args)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
                System.Diagnostics.Process.Start(psi)?.WaitForExit(4000);
            }
            catch { /* xdg-utils evtl. nicht vorhanden -> .desktop liegt trotzdem */ }
        }

        [SupportedOSPlatform("windows")]
        private static bool RegisterWindows()
        {
            try
            {
                string exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\ajfsp"))
                {
                    key.SetValue(string.Empty, "URL:ajfsp Protocol");
                    key.SetValue("URL Protocol", string.Empty);
                }
                using (var cmd = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\ajfsp\shell\open\command"))
                {
                    cmd.SetValue(string.Empty, "\"" + exe + "\" \"%1\"");
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
