using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NMH_Media_Player.Thumbnails
{
    /// <summary>
    /// Interaction logic for ThumbnailSettingsWindow.xaml
    /// </summary>
    public partial class ThumbnailSettingsWindow : Window
    {
        public int SelectedResolution { get; private set; } = 1;
        public int SelectedRows { get; private set; } = 3;
        public int SelectedColumns { get; private set; } = 4;

        private readonly TimeSpan videoDuration;

        private const int MaxRows = 12;
        private const int MaxColumns = 8;

        public ThumbnailSettingsWindow(TimeSpan videoLength)
        {
            InitializeComponent();
            videoDuration = videoLength;

            SetDefaultRowsColumns(videoLength);
        }

        private void SetDefaultRowsColumns(TimeSpan duration)
        {
            // Set default values based on video length
            if (duration.TotalMinutes <= 5)
            {
                RowsTextBox.Text = "3";
                ColumnsTextBox.Text = "4";
            }
            else if (duration.TotalMinutes <= 10)
            {
                RowsTextBox.Text = "5";
                ColumnsTextBox.Text = "4";
            }
            else if (duration.TotalMinutes <= 30)
            {
                RowsTextBox.Text = "6";
                ColumnsTextBox.Text = "5";
            }
            else if (duration.TotalMinutes <= 60)
            {
                RowsTextBox.Text = "6";
                ColumnsTextBox.Text = "5";
            }
            else if (duration.TotalMinutes <= 90)
            {
                RowsTextBox.Text = "7";
                ColumnsTextBox.Text = "5";
            }
            else if (duration.TotalMinutes <= 120)
            {
                RowsTextBox.Text = "10";
                ColumnsTextBox.Text = "5";
            }
            else if (duration.TotalMinutes <= 180)
            {
                RowsTextBox.Text = "15";
                ColumnsTextBox.Text = "5";
            }
            else
            {
                RowsTextBox.Text = "20";
                ColumnsTextBox.Text = "5";
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            // Validate resolution
            if (ResolutionComboBox.SelectedItem is ComboBoxItem resItem && int.TryParse(resItem.Tag.ToString(), out int res))
                SelectedResolution = res;

            // Validate rows
            if (!int.TryParse(RowsTextBox.Text, out int rows))
            {
                MessageBox.Show("Invalid rows number!");
                return;
            }

            // Validate columns
            if (!int.TryParse(ColumnsTextBox.Text, out int cols))
            {
                MessageBox.Show("Invalid columns number!");
                return;
            }

            if (rows > MaxRows || cols > MaxColumns)
            {
                MessageBox.Show($"Maximum allowed: {MaxRows} rows x {MaxColumns} columns.");
                return;
            }

            SelectedRows = rows;
            SelectedColumns = cols;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
    
}
