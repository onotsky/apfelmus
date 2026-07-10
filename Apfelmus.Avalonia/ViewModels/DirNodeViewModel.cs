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
    /// dafuer, dass zunaechst jeder Knoten aufklappbar erscheint.
    /// </summary>
    public sealed class DirNodeViewModel : ViewModelBase
    {
        private readonly CoreClient _client;
        private bool _loaded;
        private bool _isExpanded;

        public DirNodeViewModel(Dir dir, CoreClient client)
        {
            _client = client;
            Name = dir.Name ?? string.Empty;
            Path = dir.Path ?? string.Empty;
            Type = dir.Type;
            Children = new ObservableCollection<DirNodeViewModel> { Placeholder };
        }

        public string Name { get; }
        public string Path { get; }
        public int Type { get; }
        public ObservableCollection<DirNodeViewModel> Children { get; }

        private static DirNodeViewModel Placeholder => new(new Dir { Name = "…", Path = string.Empty }, null!);

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
            if (_loaded || _client == null) return;
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
                Children.Add(new DirNodeViewModel(d, _client));
            }
        }
    }
}
