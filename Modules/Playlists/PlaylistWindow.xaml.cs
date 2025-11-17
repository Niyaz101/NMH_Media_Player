using Microsoft.Win32;
using NMH_Media_Player.Handlers;
using NMH_Media_Player.Modules.Playlists;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NMH_Media_Player.Modules.Playlists
{
    public partial class PlaylistWindow : Window
    {

        // ================================
        // Class-level fields (accessible everywhere in this class)
        // ================================
        private readonly string playlistsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "NMH_Media_Player", "Playlists");

        private string currentPlaylistPath = string.Empty; // currently loaded playlist

        public ObservableCollection<PlaylistTrack> Tracks { get; set; } = new();



        public PlaylistWindow()
        {
            InitializeComponent();
            PlaylistGrid.ItemsSource = Tracks;
            Window_Loaded(null, null);
            

        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            // Open a SaveFileDialog for user to name the playlist
            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Create New Playlist",
                Filter = "Playlist Files (*.pls)|*.pls",
                DefaultExt = ".pls",
                InitialDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NMH_Media_Player", "Playlists")
            };


            if (saveDlg.ShowDialog() == true)
            {
                // Clear current tracks and set playlist path
                Tracks.Clear();
                currentPlaylistPath = saveDlg.FileName;

                // Create empty playlist file
                File.WriteAllText(currentPlaylistPath, string.Empty);

                // Optional: show toast instead of MessageBox
                ViewMenuHandler.ShowToast(Application.Current.MainWindow as MainWindow,
                    $"New playlist created: {Path.GetFileName(currentPlaylistPath)}");
            }
        }


        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Add Audio/Video Files to Playlist",
                Filter = "Media Files|*.mp4;*.mp3;*.wav;*.avi;*.mkv;*.wmv;*.flac;*.mov|All Files|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    Tracks.Add(new PlaylistTrack
                    {
                        Title = System.IO.Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        Duration = "Unknown"
                    });
                }

                ViewMenuHandler.ShowToast((MainWindow)Owner, $"{dlg.FileNames.Length} track(s) added to playlist.");
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistGrid.SelectedItem is PlaylistTrack selected)
                Tracks.Remove(selected);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Tracks.Count == 0)
            {
                MessageBox.Show("Playlist is empty!");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Save Playlist",
                Filter = "Playlist Files (*.pls)|*.pls",
                InitialDirectory = playlistsFolder,
                FileName = "MyPlaylist.pls"
            };

            if (dlg.ShowDialog() == true)
            {
                string json = JsonSerializer.Serialize(Tracks);
                File.WriteAllText(dlg.FileName, json);
                currentPlaylistPath = dlg.FileName;
                ViewMenuHandler.ShowToast((MainWindow)Owner, "Playlist saved successfully!");
            }
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open Playlist",
                Filter = "Playlist Files (*.pls)|*.pls",
                InitialDirectory = playlistsFolder
            };

            if (dlg.ShowDialog() == true)
            {
                string json = File.ReadAllText(dlg.FileName);
                var list = JsonSerializer.Deserialize<ObservableCollection<PlaylistTrack>>(json);
                if (list != null)
                {
                    Tracks.Clear();
                    foreach (var t in list)
                        Tracks.Add(t);
                    currentPlaylistPath = dlg.FileName;
                    ViewMenuHandler.ShowToast((MainWindow)Owner, "Playlist loaded successfully!");
                }
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (Tracks.Count == 0)
            {
                MessageBox.Show("No tracks to play!");
                return;
            }

            string firstTrack = Tracks.First().FilePath;

            // use the main controller
            var main = (MainWindow)Owner;
            main.mediaController.PlayFromPlaylist(firstTrack);

            ViewMenuHandler.ShowToast(main, $"Now playing playlist ({Tracks.Count} tracks).");
        }



        private void PlaylistGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlaylistGrid.SelectedItem is PlaylistTrack track)
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.PlayPlaylistTrack(track.FilePath);
                ViewMenuHandler.ShowToast((MainWindow)Owner, $"Now playing: {track.Title}");
            }
        }



        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(playlistsFolder); // ensure folder exists

            PlaylistSelector.Items.Clear();
            var files = Directory.GetFiles(playlistsFolder, "*.pls");
            foreach (var file in files)
            {
                PlaylistSelector.Items.Add(Path.GetFileName(file));
            }

            if (PlaylistSelector.Items.Count > 0)
                PlaylistSelector.SelectedIndex = 0; // auto-select first playlist
        }



        private void PlaylistSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistSelector.SelectedItem == null) return;

            string selectedFile = Path.Combine(playlistsFolder, PlaylistSelector.SelectedItem.ToString());
            currentPlaylistPath = selectedFile;

            Tracks.Clear();

            if (!File.Exists(selectedFile)) return;

            try
            {
                // Try reading as JSON first
                string json = File.ReadAllText(selectedFile);
                var list = JsonSerializer.Deserialize<ObservableCollection<PlaylistTrack>>(json);
                if (list != null)
                {
                    foreach (var track in list)
                        Tracks.Add(track);
                    return;
                }
            }
            catch
            {
                // If JSON parse fails, fallback to old plain file paths
                var lines = File.ReadAllLines(selectedFile);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Tracks.Add(new PlaylistTrack
                        {
                            FilePath = line,
                            Title = Path.GetFileNameWithoutExtension(line),
                            Duration = "Unknown"
                        });
                    }
                }
            }
        }



    }
}
