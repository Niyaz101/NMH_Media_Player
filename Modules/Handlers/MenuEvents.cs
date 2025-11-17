using Microsoft.Win32;
using NAudio.Midi;
using NMH_Media_Player.ColorPicker;
using NMH_Media_Player.Modules.Handlers;
using NMH_Media_Player.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;


namespace NMH_Media_Player.Modules.Handlers
{
    public static class MenuEvents
    {
        private static readonly string RecentFilesPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent.txt");

        public static MediaElement CurrentVideoPlayer;

        //------------------------ File Menu For DVD/CD Drives ------------------------//

        /// <summary>
        /// Opens media from CD/DVD drives
        /// </summary>
        public static void OpenDVD_Click(object sender, RoutedEventArgs e, MediaController mediaController)
        {
            OpenDeviceMedia(mediaController, DriveType.CDRom);
        }

        public static void OpenDevice_Click(object sender, RoutedEventArgs e, MediaController mediaController)
        {
            OpenDeviceMedia(mediaController, DriveType.Removable);
        }

        public static void OpenDisc_Click(object sender, RoutedEventArgs e, MediaController mediaController)
        {
            OpenDeviceMedia(mediaController, DriveType.Fixed);
        }

        /// <summary>
        /// Generic method for opening drives
        /// </summary>
        private static void OpenDeviceMedia(MediaController mediaController, DriveType driveType)
        {
            try
            {
                if (mediaController == null)
                {
                    MessageBox.Show("Media controller is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get all drives of the specified type
                var drives = DriveInfo.GetDrives()
                                      .Where(d => d.DriveType == driveType && d.IsReady)
                                      .ToList();

                if (drives.Count == 0)
                {
                    string typeName = driveType == DriveType.CDRom ? "CD/DVD" :
                                      driveType == DriveType.Removable ? "removable" :
                                      "fixed/internal";
                    MessageBox.Show($"No {typeName} drives detected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Pick the first drive
                var selectedDrive = drives[0];

                string[] mediaFiles;
                try
                {
                    mediaFiles = Directory.GetFiles(selectedDrive.RootDirectory.FullName)
                                          .Where(f => !string.IsNullOrWhiteSpace(f) &&
                                                      (mediaController.IsAudioFile(f) || mediaController.IsVideoFile(f)))
                                          .ToArray();
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show($"Cannot access files on drive {selectedDrive.Name}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error reading drive {selectedDrive.Name}.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (mediaFiles.Length == 0)
                {
                    MessageBox.Show("No supported media files found on the selected drive.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Set playlist and play
                var supportedFiles = mediaFiles.ToList();
                mediaController.SetPlaylist(supportedFiles);
                mediaController.PlayCurrent();

                // Save first file to recent
                RecentFileHelper.AddRecentFile(supportedFiles[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //------------------------ File Menu For Recent Files ------------------------//
        public static void RecentFiles_Click(object sender, RoutedEventArgs e, MediaController mediaController)
        {
            try
            {
                // Get only video recent files
                var recentVideos = RecentFileHelper.GetRecentFiles()
                                    .Where(f => mediaController.IsVideoFile(f))
                                    .ToList();

                if (recentVideos.Count == 0)
                {
                    MessageBox.Show("No recent video files found.", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var selectWindow = new SelectVideoWindow("Recent Videos", recentVideos)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (selectWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(selectWindow.SelectedFile))
                {
                    string selectedFile = selectWindow.SelectedFile;
                    mediaController.SetPlaylist(new List<string> { selectedFile });
                    mediaController.PlayCurrent();
                    RecentFileHelper.AddRecentFile(selectedFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent videos.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public static void RecentFilesMenu_MouseEnter(object sender, MouseEventArgs e, MediaController mediaController)
        {
            try
            {
                if (sender is not MenuItem menuItem) return;

                menuItem.Items.Clear();

                // Get only audio recent files
                var recentAudio = RecentFileHelper.GetRecentFiles()
                                    .Where(f => mediaController.IsAudioFile(f))
                                    .Reverse()
                                    .Take(10)
                                    .ToList();

                if (recentAudio.Count == 0)
                {
                    menuItem.Items.Add(new MenuItem { Header = "No recent audio files", IsEnabled = false });
                    return;
                }

                foreach (var file in recentAudio)
                {
                    bool exists = File.Exists(file);

                    var fileItem = new MenuItem
                    {
                        Header = Path.GetFileName(file),
                        ToolTip = file,
                        IsEnabled = exists
                    };

                    fileItem.Click += async (_, _) =>
                    {
                        if (!exists)
                        {
                            MessageBox.Show("File not found. Please attach the device or check the path.",
                                "File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (Application.Current.MainWindow is MainWindow main && main.mediaController != null)
                        {
                            mediaController.SetPlaylist(new List<string> { file });
                          await  mediaController.PlayCurrentWithFilterRealtimeAsync();
                            RecentFileHelper.AddRecentFile(file);
                        }
                        else
                        {
                            MessageBox.Show("Media controller not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    menuItem.Items.Add(fileItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error building recent audio files menu.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        // ------------------------ File Menu For Exit ------------------------//
        public static void Exit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic for Exit
            Application.Current.Shutdown();
        }
        // ------------------------ File Menu For Save Thumbnails ------------------------//


        // Saves a thumbnail from the video
        /// <summary>
        /// Save 10 thumbnails from a video in one combined image
        /// </summary>
        /// 
        /// 
        /// this is implemented in the ThumbnailHelper.cs file



        // ------------------------ File Menu For Properties ------------------------//
        public static void Properties_Click(string filePath, Window owner)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("No file found or loaded!");
                return;
            }

            string ffprobePath = @"C:\ffmpeg\bin\ffprobe.exe";
            if (!File.Exists(ffprobePath))
            {
                MessageBox.Show("FFprobe not found!");
                return;
            }

            try
            {
                // Run ffprobe to get JSON metadata
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string json = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        // Open PropertiesWindow and pass the JSON
                        PropertiesWindow pw = new PropertiesWindow(doc, filePath, owner);
                        pw.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading properties: " + ex.Message);
            }
        }

        // ------------------------ File Menu For Open File Location ------------------------//
        public static void OpenFileLocation_Click(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not found!");
                return;
            }

            try
            {
                string argument = $"/select,\"{filePath}\"";
                Process.Start("explorer.exe", argument);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file location: " + ex.Message);
            }
        }


        public static void MenuSelectAudioTrack_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic for Audio Track selection
            MessageBox.Show("Select Audio Track clicked!");
        }
        // ------------------------ View Menu For Subtitle Track Selection ------------------------//
        public static void MenuSelectSubtitleTrack_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic for Subtitle Track selection
            MessageBox.Show("Select Subtitle Track clicked!");
        }
        // ------------------------ View Menu For Video Quality Selection ------------------------//
        public static void MenuSelectVideoQuality_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic for Video Quality selection
            MessageBox.Show("Select Video Quality clicked!");
        }
        // ------------------------ About Menu ------------------------//
        public static void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic for About menu
            MessageBox.Show("About clicked!");
        }
        // ------------------------ File Close Menu ------------------------//
        public static void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // closes the app
        }



        public static void MenuColorChange(object sender, RoutedEventArgs e)
        {
            try
            {
                var savedColor = ThemeManager.LoadColor();

                var colorWindow = new ThemeColorWindow();
                colorWindow.Owner = Application.Current.MainWindow;

                // Public method to set initial color
                colorWindow.SetColor(savedColor);

                colorWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open Color Panel:\n{ex.Message}",
                                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
