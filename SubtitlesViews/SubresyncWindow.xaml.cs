using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NMH_Media_Player.Modules.Handlers;

namespace NMH_Media_Player.SubtitlesViews
{
    /// <summary>
    /// Interaction logic for SubresyncWindow.xaml
    /// </summary>
    public partial class SubresyncWindow : Window
    {
        public int ShiftMilliseconds { get; private set; } = 0;

        public SubresyncWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ShiftAmountBox.Text, out int ms))
            {
                ShiftMilliseconds = ms;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
