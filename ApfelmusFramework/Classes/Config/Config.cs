//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Config
{


    /// <summary>
    /// Persistente Client-Einstellungen (Verbindung, Sprache, Theme, Anzeige), die per
    /// ConfigSerializer als %AppData%\Apfelmus\Config.xml gespeichert werden. Password enthaelt den
    /// bereits als MD5-Hex abgelegten Wert (Anforderung der Core-Schnittstelle), nicht den Klartext.
    /// </summary>
    public class Config
    {
        private string hostname;

        public string HostName
        {
            get { return hostname; }
            set
            {
                hostname = value;
            }
        }

        private int port;

        public int Port
        {
            get { return port; }
            set
            {
                port = value;
            }
        }

        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        private bool hideLoginWindow;

        public bool HideLoginWindow
        {
            get { return hideLoginWindow; }
            set
            {
                hideLoginWindow = value;
            }
        }

        private bool useCompression;

        public bool UseCompression
        {
            get { return useCompression; }
            set { useCompression = value; }
        }

        private bool protocolHandler;

        public bool ProtocolHandler
        {
            get { return protocolHandler; }
            set { protocolHandler = value; }
        }

        private int refreshRate;

        public int RefreshRate
        {
            get { return refreshRate; }
            set { refreshRate = value; }
        }

        private string languageFile;

        public string LanguageFile
        {
            get { return languageFile; }
            set { languageFile = value; }
        }

        private string theme;

        public string Theme
        {
            get { return theme; }
            set { theme = value; }
        }

        private string releaseInfoHost;

        // Host fuer "Suche nach mehr Informationen" (Kontextmenue). %s wird durch den ajfsp-Link
        // ersetzt. Leer/nicht gesetzt -> ReleaseInfo.DefaultHost.
        public string ReleaseInfoHost
        {
            get { return releaseInfoHost; }
            set { releaseInfoHost = value; }
        }

        private int partlistRowHeight;

        // Hoehe (in Pixel) einer Zeile im Partlisten-Balken. Groesser = weniger, dafuer dickere
        // Zeilen -> die einzelnen Parts wirken groesser. 0 = noch nicht gesetzt (alte Config) ->
        // in MainWindow.RenderPartList wird dann ein Default verwendet.
        public int PartlistRowHeight
        {
            get { return partlistRowHeight; }
            set { partlistRowHeight = value; }
        }

        private string downloadColumnLayout;
        private string uploadColumnLayout;

        // Gespeichertes Spaltenlayout (Reihenfolge/Breite) der Download- bzw. Upload-Tabelle,
        // Format "displayIndex:breite|..." in Definitionsreihenfolge der Spalten. Leer = Standard.
        public string DownloadColumnLayout
        {
            get { return downloadColumnLayout; }
            set { downloadColumnLayout = value; }
        }

        public string UploadColumnLayout
        {
            get { return uploadColumnLayout; }
            set { uploadColumnLayout = value; }
        }

        private string gridLayouts;

        // Spaltenlayouts ALLER Tabellen in einem Feld: "name=idx:breite|...;name2=..." (je Grid eine Zeile).
        public string GridLayouts
        {
            get { return gridLayouts; }
            set { gridLayouts = value; }
        }

        private double windowWidth;
        private double windowHeight;
        private bool windowMaximized;

        // Zuletzt genutzte Fenstergroesse/-zustand (0 = noch nicht gesetzt -> Standardgroesse).
        public double WindowWidth
        {
            get { return windowWidth; }
            set { windowWidth = value; }
        }

        public double WindowHeight
        {
            get { return windowHeight; }
            set { windowHeight = value; }
        }

        public bool WindowMaximized
        {
            get { return windowMaximized; }
            set { windowMaximized = value; }
        }
    }
}
