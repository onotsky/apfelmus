using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Apfelmus.Avalonia.Services;
using Apfelmus.Avalonia.ViewModels;

namespace Apfelmus.Avalonia.Views
{
    /// <summary>
    /// Zielverzeichnis eines Downloads wählen: der Core-Verzeichnisbaum, aber gefiltert auf
    /// freigabe-relevante Zweige - freigegebene Ordner (gruen) und Elternordner, die Freigaben
    /// enthalten (gedaempftes Gruen), damit man auch zu verschachtelten Freigaben navigieren kann.
    /// Plus optionalem neuem Unterordner. Rückgabe = gewählter Core-Pfad.
    /// </summary>
    public partial class TargetDirDialog : Window
    {
        private readonly CoreClient? _client;
        private readonly HashSet<string>? _sharedPaths;
        private readonly ObservableCollection<DirNodeViewModel> _dirTree = new();

        public TargetDirDialog()
        {
            InitializeComponent();
        }

        public TargetDirDialog(CoreClient client, HashSet<string> sharedPaths) : this()
        {
            _client = client;
            _sharedPaths = sharedPaths;
            DirTreeView.ItemsSource = _dirTree;
            _ = LoadRootsAsync();
        }

        private async Task LoadRootsAsync()
        {
            if (_client == null) return;
            var aj = await _client.GetDirectoryAsync(null);
            if (aj?.Dir == null) return;
            string sep = aj.FileSystem?.Seperator ?? "/";
            foreach (var d in aj.Dir.OrderBy(x => x.Name))
            {
                if (string.IsNullOrEmpty(d.Path)) d.Path = $"{d.Name}{sep}";
                var node = new DirNodeViewModel(d, _client, _sharedPaths, prune: true);
                if (!node.IsRelevant) continue; // nur Zweige mit Freigaben
                _dirTree.Add(node);
            }
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            string basePath = (DirTreeView.SelectedItem as DirNodeViewModel)?.Path ?? string.Empty;
            string sub = NewFolderBox.Text?.Trim() ?? string.Empty;
            string result = string.IsNullOrEmpty(sub) ? basePath : basePath + sub; // basePath endet auf Trenner
            Close(string.IsNullOrWhiteSpace(result) ? null : result);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Ok_Click(this, new RoutedEventArgs()); e.Handled = true; }
            else if (e.Key == Key.Escape) { Close(null); e.Handled = true; }
            base.OnKeyDown(e);
        }
    }
}
