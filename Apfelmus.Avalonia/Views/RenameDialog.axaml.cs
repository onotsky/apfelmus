using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Apfelmus.Avalonia.Views
{
    public partial class RenameDialog : Window
    {
        public RenameDialog()
        {
            InitializeComponent();
        }

        // Enter = OK, Escape = Abbrechen.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Close(NameBox.Text); e.Handled = true; }
            else if (e.Key == Key.Escape) { Close(null); e.Handled = true; }
            base.OnKeyDown(e);
        }

        public RenameDialog(string initial) : this()
        {
            NameBox.Text = initial;
            NameBox.SelectAll();
            NameBox.Focus();
        }

        private void Ok_Click(object? sender, RoutedEventArgs e) => Close(NameBox.Text);

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
    }
}
