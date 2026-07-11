using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Apfelmus.Avalonia.Services;
using Apfelmus.Avalonia.ViewModels;
using ApfelmusFramework.Classes.Directory;

namespace Apfelmus.Avalonia.Views
{
    /// <summary>
    /// Zielverzeichnis eines Downloads wählen: die freigegebenen Ordner werden direkt als (gruene)
    /// Wurzelknoten angezeigt und lassen sich aufklappen, um in echte Unterordner zu navigieren.
    /// So sind die Freigaben garantiert sichtbar (unabhaengig vom Pfadformat des Cores). Sind keine
    /// Ordner freigegeben, wird ersatzweise der komplette Verzeichnisbaum zur Auswahl angeboten.
    /// Plus optionalem neuem Unterordner. Rückgabe = gewählter Core-Pfad.
    /// </summary>
    public partial class TargetDirDialog : Window
    {
        private readonly CoreClient? _client;
        private readonly List<string> _sharedFolders;
        private readonly HashSet<string> _sharedSet;
        private readonly string _tempPath;       // Roh-Pfad des Temp-Verzeichnisses (fuer den Extra-Wurzelknoten)
        private readonly string _tempNormalized;  // normalisiert (fuer die blaue Markierung)
        private readonly ObservableCollection<DirNodeViewModel> _dirTree = new();

        public TargetDirDialog()
        {
            InitializeComponent();
            _sharedFolders = new List<string>();
            _sharedSet = new HashSet<string>();
            _tempPath = string.Empty;
            _tempNormalized = string.Empty;
        }

        public TargetDirDialog(CoreClient client, IEnumerable<string> sharedFolders, string? tempPath = null) : this()
        {
            _client = client;
            _sharedFolders = sharedFolders?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList() ?? new List<string>();
            _sharedSet = new HashSet<string>(_sharedFolders.Select(DirNodeViewModel.NormalizePath));
            _tempPath = tempPath?.Trim() ?? string.Empty;
            _tempNormalized = DirNodeViewModel.NormalizePath(_tempPath);
            DirTreeView.ItemsSource = _dirTree;
            _ = LoadRootsAsync();
        }

        private async Task LoadRootsAsync()
        {
            if (_client == null) return;

            // Bevorzugt: die freigegebenen Ordner direkt als gruene, aufklappbare Wurzeln zeigen.
            if (_sharedFolders.Count > 0)
            {
                foreach (var share in _sharedFolders)
                {
                    var d = new Dir { Name = share, Path = share };
                    _dirTree.Add(new DirNodeViewModel(d, _client, _sharedSet, prune: false, tempPath: _tempNormalized));
                }
                // Temp-Verzeichnis zusaetzlich (blau) anbieten, sofern gesetzt und nicht ohnehin unter einer Freigabe.
                if (_tempNormalized.Length > 0 && !IsUnderShare(_tempNormalized))
                {
                    var t = new Dir { Name = _tempPath, Path = _tempPath };
                    _dirTree.Add(new DirNodeViewModel(t, _client, _sharedSet, prune: false, tempPath: _tempNormalized));
                }
                return;
            }

            // Fallback (keine Freigaben konfiguriert): kompletten Verzeichnisbaum zur Auswahl anbieten.
            var aj = await _client.GetDirectoryAsync(null);
            if (aj?.Dir == null) return;
            string sep = aj.FileSystem?.Seperator ?? "/";
            foreach (var d in aj.Dir.OrderBy(x => x.Name))
            {
                if (string.IsNullOrEmpty(d.Path)) d.Path = $"{d.Name}{sep}";
                _dirTree.Add(new DirNodeViewModel(d, _client, _sharedSet, prune: false, tempPath: _tempNormalized));
            }
        }

        // Liegt der Pfad auf oder unter einer der Freigaben (dann ist er im Baum schon erreichbar)?
        private bool IsUnderShare(string np)
            => _sharedSet.Any(s => np == s || np.StartsWith(s + "/", System.StringComparison.Ordinal));

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            string basePath = (DirTreeView.SelectedItem as DirNodeViewModel)?.Path ?? string.Empty;
            string sub = NewFolderBox.Text?.Trim() ?? string.Empty;

            string result = basePath;
            if (!string.IsNullOrEmpty(sub))
            {
                // Trenner des Zielsystems aus dem Pfad ableiten und sicher genau einmal einfuegen.
                char sep = basePath.Contains('\\') ? '\\' : '/';
                if (basePath.Length > 0 && basePath[basePath.Length - 1] != sep) basePath += sep;
                result = basePath + sub;
            }
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
