//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Config
{


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

        private int partlistRowHeight;

        // Hoehe (in Pixel) einer Zeile im Partlisten-Balken. Groesser = weniger, dafuer dickere
        // Zeilen -> die einzelnen Parts wirken groesser. 0 = noch nicht gesetzt (alte Config) ->
        // in MainWindow.RenderPartList wird dann ein Default verwendet.
        public int PartlistRowHeight
        {
            get { return partlistRowHeight; }
            set { partlistRowHeight = value; }
        }
    }
}
