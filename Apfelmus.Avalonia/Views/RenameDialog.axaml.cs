using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Apfelmus.Avalonia.Views
{
    public partial class RenameDialog : Window
    {
        public RenameDialog()
        {
            InitializeComponent();
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
