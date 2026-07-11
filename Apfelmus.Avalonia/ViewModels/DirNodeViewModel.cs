using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Apfelmus.Avalonia.Services;
using ApfelmusFramework.Classes.Directory;

namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Knoten des Freigabe-Verzeichnisbaums. Kinder werden erst beim Aufklappen vom Core geladen
    /// (lazy, directory.xml) - analog zur WPF-DirectoryChildren-Logik. Ein Platzhalter-Kind sorgt
    /// dafuer, dass zunaechst jeder Knoten aufklappbar erscheint; beim Aufklappen wird es durch die
    /// echten Unterordner ersetzt.
    /// </summary>
    public sealed class DirNodeViewModel : ViewModelBase
    {
        private readonly CoreClient? _client;
        private readonly bool _isPlaceholder;
        private readonly System.Collections.Generic.HashSet<string>? _sharedPaths;
        private readonly bool _prune;
        private bool _loaded;
        private bool _isExpanded;

        /// <summary>
        /// Echter Verzeichnisknoten (bekommt einen Platzhalter, um aufklappbar zu sein).
        /// <paramref name="prune"/> = nur freigabe-relevante Kinder anzeigen (Zielverzeichnis-Dialog).
        /// </summary>
        public DirNodeViewModel(Dir dir, CoreClient client,
                                System.Collections.Generic.HashSet<string>? sharedPaths = null, bool prune = false)
        {
            _client = client;
            _sharedPaths = sharedPaths;
            _prune = prune;
            Name = dir.Name ?? string.Empty;
            Path = dir.Path ?? string.Empty;
            Type = dir.Type;

            string np = NormalizePath(Path);
            if (sharedPaths != null && sharedPaths.Count > 0 && np.Length > 0)
            {
                // freigegeben oder INNERHALB einer Freigabe:
                IsSharedContent = sharedPaths.Any(s => np == s || np.StartsWith(s + "/", System.StringComparison.Ordinal));
                // Vorfahr einer Freigabe (selbst nicht freigegeben, aber darunter liegt eine):
                HasSharedBelow = sharedPaths.Any(s => s.StartsWith(np + "/", System.StringComparison.Ordinal));
            }
            Children = new ObservableCollection<DirNodeViewModel> { new DirNodeViewModel() };
        }

        /// <summary>Freigegeben oder innerhalb einer Freigabe (gruene Markierung, fett).</summary>
        public bool IsSharedContent { get; }
        /// <summary>Enthaelt weiter unten Freigaben, ist selbst aber nicht freigegeben (indirekte Markierung).</summary>
        public bool HasSharedBelow { get; }
        /// <summary>Fuer den (gefilterten) Zielverzeichnis-Dialog: nur relevante Zweige zeigen.</summary>
        public bool IsRelevant => IsSharedContent || HasSharedBelow;

        // Icon-Auswahl im Baum-Template.
        public bool ShowSharedIcon => IsSharedContent;
        public bool ShowAncestorIcon => HasSharedBelow && !IsSharedContent;
        public bool ShowPlainIcon => !IsSharedContent && !HasSharedBelow;

        /// <summary>Vergleichsform eines Ordnerpfads (ohne Rand-Trenner, Slash-normalisiert, kleingeschrieben).</summary>
        public static string NormalizePath(string? p)
            => (p ?? string.Empty).Replace('\\', '/').Trim('/').ToLowerInvariant();

        /// <summary>Platzhalter-Knoten (KEINE Kinder - verhindert die Endlos-Rekursion).</summary>
        private DirNodeViewModel()
        {
            _isPlaceholder = true;
            Name = "…";
            Path = string.Empty;
            Children = new ObservableCollection<DirNodeViewModel>();
        }

        public string Name { get; }
        public string Path { get; }
        public int Type { get; }
        public ObservableCollection<DirNodeViewModel> Children { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value) && value)
                {
                    _ = LoadChildrenAsync();
                }
            }
        }

        private async Task LoadChildrenAsync()
        {
            if (_loaded || _isPlaceholder || _client == null) return;
            _loaded = true;

            var aj = await _client.GetDirectoryAsync(Path);
            Children.Clear();
            if (aj?.Dir == null) return;

            string sep = aj.FileSystem?.Seperator ?? "/";
            foreach (var d in aj.Dir.OrderBy(x => x.Name))
            {
                if (string.IsNullOrEmpty(d.Path))
                {
                    d.Path = sep == "/"
                        ? $"{Path}{d.Name}{sep}"
                        : $"{Path}{sep}{d.Name}";
                }
                var node = new DirNodeViewModel(d, _client, _sharedPaths, _prune);
                if (_prune && !node.IsRelevant) continue; // im Dialog nur freigabe-relevante Zweige
                Children.Add(node);
            }
        }
    }
}
