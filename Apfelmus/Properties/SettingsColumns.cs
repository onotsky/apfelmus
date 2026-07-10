namespace Apfelmus.Properties
{
    /// <summary>
    /// Erweitert die vom Designer generierte Settings-Klasse (Settings.Designer.cs) um zwei
    /// benutzerbezogene Eintraege fuer das gespeicherte Spaltenlayout (Reihenfolge + Breite) der
    /// Download- und Upload-Tabelle. Bewusst als separate partielle Datei, damit ein erneutes
    /// Generieren der Designer-Datei diese Ergaenzungen nicht ueberschreibt. Wie die
    /// Fenstergeometrie werden die Werte benutzerbezogen in der user.config persistiert.
    /// </summary>
    internal sealed partial class Settings
    {
        [global::System.Configuration.UserScopedSetting()]
        [global::System.Configuration.DefaultSettingValue("")]
        public string DownloadColumnLayout
        {
            get { return (string)this["DownloadColumnLayout"]; }
            set { this["DownloadColumnLayout"] = value; }
        }

        [global::System.Configuration.UserScopedSetting()]
        [global::System.Configuration.DefaultSettingValue("")]
        public string UploadColumnLayout
        {
            get { return (string)this["UploadColumnLayout"]; }
            set { this["UploadColumnLayout"] = value; }
        }
    }
}
