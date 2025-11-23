using Microsoft.Win32;
using NMH_Media_Player.Modules; // FileManager
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NMH_Media_Player.Modules.Handlers
{
    public static class FileMenuHandler
    {
        #region ------------------- Open Media -------------------

        // Generic helper to open media files repeated in the bellow three methods.
        private static  void OpenMediaFiles(MainWindow window, List<string> files)
        {
            if (files == null || files.Count == 0) return;

            window.mediaController.SetPlaylist(files);
            window.mediaController.PlayCurrent();
        }

        public static  void BtnOpen_Click(MainWindow window)
        {
            var files = FileManager.OpenFiles();
            OpenMediaFiles(window, files);
        }

        public static  void OpenDirectory_Click(MainWindow window)
        {
            var files = FileManager.OpenDirectory();
            OpenMediaFiles(window, files);
        }

        public static  void OpenFileUrl_Click(MainWindow window)
        {
            var urlWindow = new InputBoxWindow("Enter File URL", "Please enter the media URL:")
            {
                Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (urlWindow.ShowDialog() != true) return;

            string url = urlWindow.InputText;
            if (!FileManager.IsSupportedMediaUrl(url))
            {
                MessageBox.Show("Unsupported media URL.", "Error");
                return;
            }

            window.mediaController.AddToPlaylist(url);
             window.mediaController.PlayCurrent();
        }

        public static  void QuickOpenFile_Click(object sender, RoutedEventArgs e, MediaController mediaController)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = "Quick Open File",
                    Filter = "Supported Media Files|*.mp4;*.mp3;*.wav;*.flac;*.mkv|All Files|*.*",
                    Multiselect = true
                };

                if (dlg.ShowDialog() != true || dlg.FileNames.Length == 0) return;

                List<string> files = dlg.FileNames
                    .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                    .ToList();

                if (files.Count == 0)
                {
                    MessageBox.Show("No valid media files selected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (mediaController == null)
                {
                    MessageBox.Show("Media controller is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                mediaController.SetPlaylist(files);
                 mediaController.PlayCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ------------------- Save Media -------------------

        public static void SaveCopy_Click(MainWindow window)
        {
            try
            {
                string currentFile = window.mediaController.CurrentFile;
                if (string.IsNullOrEmpty(currentFile) || !File.Exists(currentFile))
                {
                    MessageBox.Show(window, "No file currently playing!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    FileName = Path.GetFileName(currentFile),
                    Filter = "All Files|*.*"
                };

                if (dlg.ShowDialog(window) != true) return;

                if (FileManager.SaveCopy(currentFile, dlg.FileName, out string error))
                    MessageBox.Show(window, $"File copy saved successfully:\n{dlg.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(window, error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, $"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void SaveImage_Click(MainWindow window)
        {
            try
            {
                if (window.mediaPlayer.Source == null)
                {
                    MessageBox.Show(window, "No video currently playing!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    FileName = "snapshot.png",
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg"
                };

                if (dlg.ShowDialog(window) != true) return;

                var rtb = new RenderTargetBitmap(
                    (int)window.mediaPlayer.ActualWidth,
                    (int)window.mediaPlayer.ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                rtb.Render(window.mediaPlayer);

                bool isPng = Path.GetExtension(dlg.FileName).ToLower() == ".png";

                if (FileManager.SaveImage(rtb, dlg.FileName, isPng, out string error))
                    MessageBox.Show(window, $"Snapshot saved successfully:\n{dlg.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(window, error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, $"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
