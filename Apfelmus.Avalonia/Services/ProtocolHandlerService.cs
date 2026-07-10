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
        public static bool IsSupported => OperatingSystem.IsWindows();

        public static bool Register()
        {
            if (!OperatingSystem.IsWindows()) return false;
            return RegisterWindows();
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
