using System.Collections.ObjectModel;
using ApfelmusFramework.Classes.Modified;
using Apfelmus.Avalonia.Services;

namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Eine laufende/abgeschlossene Suche als eigener Ergebnis-Tab (analog zum WPF-CloseableTabItem).
    /// Haelt Suchtext, Core-Such-Id, Treffer/Quellen-Zaehler, den Laufstatus und die Ergebnisliste.
    /// </summary>
    public sealed class SearchTabViewModel : ViewModelBase
    {
        public SearchTabViewModel(string title)
        {
            Title = title;
            Results = new ObservableCollection<SearchEntry>();
        }

        public string Title { get; }
        public int Id { get; set; }

        // True, sobald der Core die Suche mind. einmal in seiner Liste gemeldet hat. Verschwindet sie
        // danach wieder, gilt die Suche als beendet (Running -> false) - so haengt der Status nicht.
        public bool Seen { get; set; }

        public ObservableCollection<SearchEntry> Results { get; }

        private SearchEntry? _selectedResult;
        public SearchEntry? SelectedResult { get => _selectedResult; set => SetProperty(ref _selectedResult, value); }

        private int _foundFiles;
        public int FoundFiles { get => _foundFiles; set => SetProperty(ref _foundFiles, value); }

        private int _sumSearches;
        public int SumSearches { get => _sumSearches; set => SetProperty(ref _sumSearches, value); }

        private bool _running;
        public bool Running
        {
            get => _running;
            set { if (SetProperty(ref _running, value)) OnPropertyChanged(nameof(StatusText)); }
        }

        /// <summary>Sichtbarer Laufstatus fuer die Kopfzeile des Ergebnis-Tabs.</summary>
        public string StatusText => LanguageManager.Get(Running ? "s_running" : "s_done");
    }
}
