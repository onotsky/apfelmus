using System.Windows;

namespace Apfelmus
{
    /// <summary>
    /// Kleines, themekonformes Meldungsfenster als Ersatz fuer die (ungetheimte) MessageBox.
    /// Wird u.a. fuer die Rueckmeldung der Link-Uebernahme genutzt ("Datei bereits geladen" usw.).
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow(string message, string title)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(title))
                this.Title = title;

            txtMessage.Text = message;
            btnOk.Focus();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
