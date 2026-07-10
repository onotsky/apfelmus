using Avalonia.Controls;
using Avalonia.Interactivity;
using Apfelmus.Avalonia.ViewModels;

namespace Apfelmus.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // InitializeComponent wird vom Avalonia-XAML-Compiler generiert (partial).
            InitializeComponent();
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is MainWindowViewModel vm)
            {
                // Kopier-Anfragen des ViewModels in die Zwischenablage legen (Clipboard haengt am TopLevel).
                vm.CopyRequested -= OnCopyRequested;
                vm.CopyRequested += OnCopyRequested;
            }
        }

        private async void OnCopyRequested(string text)
        {
            var clipboard = Clipboard;
            if (clipboard != null && !string.IsNullOrEmpty(text))
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }
}
