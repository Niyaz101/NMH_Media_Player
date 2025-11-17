using LibVLCSharp.Shared;
using NMH_Media_Player.Modules.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace NMH_Media_Player
{
    public partial class SelectVideoWindow : Window
    {
        // Nullable at first, will be set when user selects a video
        public string? SelectedFile { get; private set; }

        // Full paths of the videos
        private readonly List<string> fullPaths;

        public SelectVideoWindow(string title, List<string> files)
        {
            InitializeComponent();

            this.Title = title;

            // Assign full paths
            fullPaths = files ?? new List<string>();

            // Clear existing items and bind file names
            VideoListBox.ItemsSource = null;
            VideoListBox.Items.Clear();

            foreach (var file in fullPaths)
            {
                VideoListBox.Items.Add(Path.GetFileName(file));
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Only proceed if an item is selected
            if (VideoListBox.SelectedIndex >= 0)
            {
                SelectedFile = fullPaths[VideoListBox.SelectedIndex]; // use full path
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a video to play.", "Select Video",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile = null;
            DialogResult = false;
            Close();
        }

        private void VideoListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only proceed if double-clicked item is valid
            if (VideoListBox.SelectedIndex >= 0)
            {
                SelectedFile = fullPaths[VideoListBox.SelectedIndex]; // use full path
                DialogResult = true;
                Close();
            }
        }
    }
}
