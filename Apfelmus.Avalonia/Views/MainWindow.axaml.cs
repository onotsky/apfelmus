using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Apfelmus.Avalonia.ViewModels;

namespace Apfelmus.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private bool _columnsRestored;

        public MainWindow()
        {
            // InitializeComponent wird vom Avalonia-XAML-Compiler generiert (partial).
            InitializeComponent();
        }

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);
            if (_columnsRestored || DataContext is not MainWindowViewModel vm) return;
            _columnsRestored = true;
            RestoreColumns(this.FindControl<DataGrid>("DownloadsGrid"), vm.DownloadColumnLayout);
            RestoreColumns(this.FindControl<DataGrid>("UploadsGrid"), vm.UploadColumnLayout);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SaveColumnLayouts(
                    SerializeColumns(this.FindControl<DataGrid>("DownloadsGrid")),
                    SerializeColumns(this.FindControl<DataGrid>("UploadsGrid")));
            }
            base.OnClosing(e);
        }

        // Serialisiert je Spalte "DisplayIndex:Breite" in Definitionsreihenfolge.
        private static string SerializeColumns(DataGrid? grid)
        {
            if (grid == null) return string.Empty;
            var parts = new List<string>();
            foreach (var c in grid.Columns)
            {
                double w = c.ActualWidth > 0 ? c.ActualWidth : (c.Width.IsAbsolute ? c.Width.Value : 0);
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1:0.#}", c.DisplayIndex, w));
            }
            return string.Join("|", parts);
        }

        // Stellt Breite (ausser Sternspalten) und Reihenfolge wieder her; bei geaenderter Spaltenzahl ignoriert.
        private static void RestoreColumns(DataGrid? grid, string? layout)
        {
            if (grid == null || string.IsNullOrWhiteSpace(layout)) return;
            try
            {
                var parts = layout.Split('|');
                if (parts.Length != grid.Columns.Count) return;
                var order = new List<(DataGridColumn col, int idx)>();
                for (int i = 0; i < parts.Length; i++)
                {
                    var kv = parts[i].Split(':');
                    if (kv.Length != 2
                        || !int.TryParse(kv[0], out int di)
                        || !double.TryParse(kv[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double w))
                        return;
                    var col = grid.Columns[i];
                    if (!col.Width.IsStar && w > 10) col.Width = new DataGridLength(w);
                    order.Add((col, di));
                }
                // DisplayIndex in Zielreihenfolge setzen (vermeidet Kollisionen).
                foreach (var (col, idx) in order.OrderBy(x => x.idx))
                    if (idx >= 0 && idx < grid.Columns.Count) col.DisplayIndex = idx;
            }
            catch { }
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is MainWindowViewModel vm)
            {
                // Kopier-Anfragen des ViewModels in die Zwischenablage legen (Clipboard haengt am TopLevel).
                vm.CopyRequested -= OnCopyRequested;
                vm.CopyRequested += OnCopyRequested;
                vm.RenameRequested -= OnRenameRequested;
                vm.RenameRequested += OnRenameRequested;
            }
        }

        private async void OnRenameRequested(ApfelmusFramework.Classes.Modified.Download d)
        {
            var dlg = new RenameDialog(d.FileName ?? string.Empty);
            var result = await dlg.ShowDialog<string?>(this);
            if (result != null && DataContext is MainWindowViewModel vm)
            {
                vm.ExecuteRename(d.Id, result);
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

        // ---- Mein Share: Mehrfachauswahl ----
        // Avalonias DataGrid.SelectedItems ist nicht bindbar; die markierten Freigaben werden
        // daher hier ausgelesen und dem ViewModel zum Kopieren der ajfsp-Links uebergeben.
        private void CopyShareLinks_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm && SharesGrid?.SelectedItems is { Count: > 0 } sel)
            {
                vm.CopyShareLinks(sel);
            }
        }

        // ---- Eigene Titelleiste ----
        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e) => ToggleMaximize();

        private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaxRestore_Click(object? sender, RoutedEventArgs e) => ToggleMaximize();

        private void Close_Click(object? sender, RoutedEventArgs e) => Close();

        private void ToggleMaximize()
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
