using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Apfelmus.Avalonia.ViewModels;

namespace Apfelmus.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private bool _columnsRestored;
        private bool _sizeRestored;

        public MainWindow()
        {
            // InitializeComponent wird vom Avalonia-XAML-Compiler generiert (partial).
            InitializeComponent();
            // Doppelklick auf einen Spaltentrenner -> Spalte an den Inhalt anpassen (wie Excel/WPF).
            AddHandler(Gestures.DoubleTappedEvent, OnGridSeparatorDoubleTapped, RoutingStrategies.Bubble);
        }

        private static readonly PropertyInfo? OwningColumnProp =
            typeof(DataGridColumnHeader).GetProperty("OwningColumn", BindingFlags.NonPublic | BindingFlags.Instance);

        private void OnGridSeparatorDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (e.Source is not Visual v) return;
            var header = v as DataGridColumnHeader ?? v.FindAncestorOfType<DataGridColumnHeader>();
            var grid = header?.FindAncestorOfType<DataGrid>();
            if (header == null || grid == null) return;
            if (OwningColumnProp?.GetValue(header) is not DataGridColumn col) return;

            // Avalonia hat keine benannten Resize-Griffe: der Trenner ist der schmale Randbereich
            // des Spaltenkopfes. Doppelklick am rechten Rand -> diese Spalte, am linken -> die vorige.
            var pos = e.GetPosition(header);
            const double edge = 9;
            DataGridColumn target;
            if (pos.X >= header.Bounds.Width - edge) target = col;
            else if (pos.X <= edge) target = PreviousVisibleColumn(grid, col) ?? col;
            else return; // Mitte des Kopfes -> das ist Sortieren, nicht anpassen

            // Auto = an den breitesten Inhalt (inkl. Kopf) anpassen.
            target.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            e.Handled = true;
        }

        private static DataGridColumn? PreviousVisibleColumn(DataGrid grid, DataGridColumn col)
        {
            DataGridColumn? best = null;
            int bestIdx = -1;
            foreach (var c in grid.Columns)
                if (c.IsVisible && c.DisplayIndex < col.DisplayIndex && c.DisplayIndex > bestIdx)
                {
                    best = c;
                    bestIdx = c.DisplayIndex;
                }
            return best;
        }

        // Alle Tabellen, deren Spaltenlayout gespeichert wird.
        private static readonly string[] GridNames =
            { "DownloadsGrid", "SourcesGrid", "UploadsGrid", "ServersGrid", "SharesGrid" };

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);
            if (_columnsRestored || DataContext is not MainWindowViewModel vm) return;
            _columnsRestored = true;
            var map = ParseGridLayouts(vm.SavedGridLayouts);
            foreach (var name in GridNames)
                if (map.TryGetValue(name, out var layout))
                    RestoreColumns(this.FindControl<DataGrid>(name), layout);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var name in GridNames)
                {
                    var g = this.FindControl<DataGrid>(name);
                    if (g == null) continue;
                    string layout = SerializeColumns(g);
                    if (!string.IsNullOrEmpty(layout)) sb.Append(name).Append('=').Append(layout).Append('\n');
                }
                vm.SaveWindowAndGrids(sb.ToString(), Width, Height, WindowState == WindowState.Maximized);
            }
            base.OnClosing(e);
        }

        private static Dictionary<string, string> ParseGridLayouts(string s)
        {
            var map = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(s)) return map;
            foreach (var line in s.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) map[line.Substring(0, eq)] = line.Substring(eq + 1);
            }
            return map;
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
                vm.TargetDirRequested -= OnTargetDirRequested;
                vm.TargetDirRequested += OnTargetDirRequested;
                vm.ActivateRequested -= OnActivateRequested;
                vm.ActivateRequested += OnActivateRequested;

                // Fenstergroesse wie zuletzt geschlossen (DataContext wird VOR dem Anzeigen gesetzt).
                if (!_sizeRestored)
                {
                    _sizeRestored = true;
                    if (vm.SavedWindowWidth > 200 && vm.SavedWindowHeight > 150)
                    {
                        Width = vm.SavedWindowWidth;
                        Height = vm.SavedWindowHeight;
                    }
                    if (vm.SavedWindowMaximized) WindowState = WindowState.Maximized;
                }
            }
        }

        // Bei Link-Uebergabe: Fenster nach vorne holen (und aus dem Minimieren zurueckholen).
        private void OnActivateRequested()
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                Show();
                Activate();
            });
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

        private async void OnTargetDirRequested(ApfelmusFramework.Classes.Modified.Download d)
        {
            // Der Zielpfad liegt auf dem Core-Rechner -> Texteingabe (kein lokaler Ordner-Dialog).
            var dlg = new RenameDialog(d.TargetDirectory ?? string.Empty) { Title = "Zielverzeichnis" };
            var result = await dlg.ShowDialog<string?>(this);
            if (result != null && DataContext is MainWindowViewModel vm)
            {
                vm.ExecuteSetTargetDir(d.Id, result);
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

        // ---- Partlisten-Hover-Tooltip (Typ unter dem Cursor, wie WPF) ----
        private void Partlist_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is Control c && DataContext is MainWindowViewModel vm)
            {
                var pos = e.GetPosition(c);
                double w = c.Bounds.Width, h = c.Bounds.Height;
                string? text = (w > 0 && h > 0) ? vm.GetPartlistTooltip(pos.X / w, pos.Y / h) : null;
                if (!string.IsNullOrEmpty(text))
                {
                    ToolTip.SetTip(c, text);
                    ToolTip.SetIsOpen(c, true);
                }
                else
                {
                    ToolTip.SetIsOpen(c, false);
                }
            }
        }

        private void Partlist_PointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is Control c) ToolTip.SetIsOpen(c, false);
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

        private void CopyShareSources_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm && SharesGrid?.SelectedItems is { Count: > 0 } sel)
            {
                vm.CopyShareSources(sel);
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
