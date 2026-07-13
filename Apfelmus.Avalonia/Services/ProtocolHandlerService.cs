using System;
using System.Runtime.Versioning;

namespace Apfelmus.Avalonia.Services
{
    /// <summary>
    /// Registriert das ajfsp://-Protokoll unter Windows/Linux fuer diese Anwendung, damit Browser
    /// (z.B. Chrome) ajfsp-Links an Apfelmus uebergeben. Bewusst pro Benutzer (Windows:
    /// HKCU\Software\Classes, Linux: ~/.local/share) - keine Adminrechte noetig.
    ///
    /// Unter Linux sorgt <see cref="EnsureLinuxDesktopEntry"/> zusaetzlich fuer das
    /// App-Icon in Dock/Taskleiste/Alt-Tab: GNOME (Wayland) ordnet das laufende Fenster
    /// ueber die app_id (X11: WM_CLASS) einer .desktop-Datei zu und nimmt deren Icon= -
    /// ohne passende .desktop mit Icon zeigt die Shell nur ein generisches Fallback-Icon.
    /// </summary>
    public static class ProtocolHandlerService
    {
        // Basisname der .desktop MUSS zur Wayland-app_id / X11-WM_CLASS passen (= Assembly-/Exe-Name),
        // damit GNOME das Fenster der Launcher-Datei (und damit dem Icon) zuordnet.
        private const string AppId = "Apfelmus.Avalonia";
        private const string DesktopName = AppId + ".desktop";
        private const string LegacyDesktopName = "apfelmus-ajfsp.desktop";

        public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

        public static bool Register()
        {
            if (OperatingSystem.IsWindows()) return RegisterWindows();
            if (OperatingSystem.IsLinux()) return RegisterLinux(registerScheme: true);
            return false;
        }

        /// <summary>
        /// Installiert unter Linux die Launcher-.desktop samt Icon (idempotent), OHNE das
        /// ajfsp-Schema als Standard zu setzen. Beim Start aufrufen, damit das App-Icon auch
        /// ohne aktivierte ajfsp-Verknuepfung erscheint. Auf Nicht-Linux ein No-Op.
        /// </summary>
        public static void EnsureLinuxDesktopEntry()
        {
            if (OperatingSystem.IsLinux()) RegisterLinux(registerScheme: false);
        }

        [SupportedOSPlatform("linux")]
        private static bool RegisterLinux(bool registerScheme)
        {
            try
            {
                string exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;

                // Desktop-Handler gehoeren nach $XDG_DATA_HOME/applications (~/.local/share/applications),
                // NICHT nach ~/.config. LocalApplicationData = ~/.local/share unter Linux.
                string dataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appsDir = System.IO.Path.Combine(dataDir, "applications");
                System.IO.Directory.CreateDirectory(appsDir);

                // Eingebettetes Icon nach ~/.local/share/icons entpacken und per absolutem Pfad
                // referenzieren (absolute Pfade in Icon= sind groessenunabhaengig und robust).
                string iconPath = ExtractIcon(dataDir);

                string desktopPath = System.IO.Path.Combine(appsDir, DesktopName);
                string iconLine = iconPath.Length > 0 ? "Icon=" + iconPath + "\n" : string.Empty;

                // Vollwertiger Launcher (im Menue sichtbar) + ajfsp-Handler in einer Datei.
                // %u uebergibt einen geklickten ajfsp-Link als Argument.
                string content =
                    "[Desktop Entry]\n" +
                    "Type=Application\n" +
                    "Name=Apfelmus\n" +
                    "Comment=appleJuice P2P-Client\n" +
                    "Exec=\"" + exe + "\" %u\n" +
                    iconLine +
                    "Terminal=false\n" +
                    "Categories=Network;FileTransfer;P2P;\n" +
                    "StartupWMClass=" + AppId + "\n" +
                    "MimeType=x-scheme-handler/ajfsp;\n";
                System.IO.File.WriteAllText(desktopPath, content);

                // Alte, rein versteckte Handler-Datei aufraeumen (wurde durch die Launcher-Datei ersetzt).
                string legacy = System.IO.Path.Combine(appsDir, LegacyDesktopName);
                try { if (System.IO.File.Exists(legacy)) System.IO.File.Delete(legacy); } catch { }

                Run("update-desktop-database", appsDir);
                if (registerScheme)
                    Run("xdg-mime", "default " + DesktopName + " x-scheme-handler/ajfsp");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Entpackt das eingebettete App-Icon nach ~/.local/share/icons/Apfelmus.Avalonia.png
        /// (nur wenn noch nicht vorhanden) und liefert dessen absoluten Pfad, sonst "".
        /// </summary>
        [SupportedOSPlatform("linux")]
        private static string ExtractIcon(string dataDir)
        {
            try
            {
                string iconsDir = System.IO.Path.Combine(dataDir, "icons");
                System.IO.Directory.CreateDirectory(iconsDir);
                string iconPath = System.IO.Path.Combine(iconsDir, AppId + ".png");
                if (!System.IO.File.Exists(iconPath))
                {
                    var uri = new Uri("avares://Apfelmus.Avalonia/Assets/Images/Apple-icon.png");
                    using var src = global::Avalonia.Platform.AssetLoader.Open(uri);
                    using var dst = System.IO.File.Create(iconPath);
                    src.CopyTo(dst);
                }
                return iconPath;
            }
            catch { return string.Empty; }
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
