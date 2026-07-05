using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Apfelmus
{
    /// <summary>
    /// Interaktionslogik für TargetdirWindow.xaml
    /// </summary>
    public partial class TargetdirWindow : Window
    {
        public string TargetDir
        {
            get;
            set;
        }

        public TargetdirWindow()
        {
            InitializeComponent();
        }

        private void btnOk_Click_1(object sender, RoutedEventArgs e)
        {
            if (tbxTargetDir.Text.Length > 0)
            {
                this.DialogResult = true;
                TargetDir = tbxTargetDir.Text;
                this.Close();
            }
            else
            {
                tbxTargetDir.BorderThickness = new Thickness(2, 2, 2, 2);
                tbxTargetDir.BorderBrush = Brushes.Red;
            }
        }

        private void btnCancel_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
