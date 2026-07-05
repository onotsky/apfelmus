using System.Windows;
using System.ComponentModel;

namespace Apfelmus
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class OwnSplashScreen : Window
    {

        BackgroundWorker bWorker = new BackgroundWorker();

        public OwnSplashScreen()
        {

            InitializeComponent();
            InitializeBackgroundWorker();
        }

        private void InitializeBackgroundWorker()
        {
            bWorker.DoWork += new DoWorkEventHandler(bWorker_DoWork);
            bWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bWorker_RunWorkerCompleted);
            bWorker.ProgressChanged += new ProgressChangedEventHandler(bWorker_ProgressChanged);
            bWorker.WorkerReportsProgress = true;
            bWorker.RunWorkerAsync();
        }

        private void bWorker_DoWork(object sender,
            DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;

            for (int i = 1; i <= 10; i++)
            {

                // Perform a time consuming operation and report progress.
                System.Threading.Thread.Sleep(400);
                worker.ReportProgress(i * 10);

            }

        }

        private void bWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }

        private void bWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.pBarSplash.Value = e.ProgressPercentage;
        }
    }


}
