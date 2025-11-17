using System.Collections.Generic;
using System.Windows;

namespace NMH_Media_Player.Modules.Handlers
{
    public partial class SelectFileWindow : Window
    {
        public string SelectedFile { get; private set; }

        public SelectFileWindow(string title, List<string> files)
        {
            InitializeComponent();
            this.Title = title;
            listBoxFiles.ItemsSource = files;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxFiles.SelectedItem != null)
            {
                SelectedFile = listBoxFiles.SelectedItem.ToString();
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
