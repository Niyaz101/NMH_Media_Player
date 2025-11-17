using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows;  // Required for MessageBox


namespace NMH_Media_Player.Modules
{
    public static class FileManager
    {
        // ========================= Open Files =========================
        public static List<string> OpenFiles()
        {
            // Fully qualified to avoid ambiguity
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Media Files|*.mp4;*.mp3;*.avi;*.wmv;*.mkv;*.wav;*.flac;*.m4a;*.aac;*.ogg|All Files|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
                return dlg.FileNames.ToList();

            return new List<string>();
        }

        // ========================= Open Directory =========================
        public static List<string> OpenDirectory()
        {
            // Fully qualified to avoid ambiguity
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return System.IO.Directory
                        .GetFiles(fbd.SelectedPath)
                        .Where(f => IsSupportedMediaUrl(f))
                        .ToList();
                }
            }

            return new List<string>();
        }

        // ========================= Supported Media Check =========================
        public static bool IsSupportedMediaUrl(string url)
        {
            string[] exts = { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg", ".mp4", ".avi", ".wmv", ".mkv" };
            string ext = System.IO.Path.GetExtension(url).ToLowerInvariant();
            return exts.Contains(ext);
        }

        // ========================= File Operations =========================
        /// <summary>
        /// Saves a copy of the media file to the specified destination.
        /// Handles nulls, non-existent files, and exceptions.
        /// </summary>
        public static bool SaveCopy(string sourcePath, string destinationPath, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                error = "No media file is currently loaded or playing.";
                return false;
            }

            if (!File.Exists(sourcePath))
            {
                error = "Source file does not exist!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                error = "Destination path is invalid.";
                return false;
            }

            try
            {
                File.Copy(sourcePath, destinationPath, true);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Access denied. Cannot save the file.";
            }
            catch (IOException ioEx)
            {
                error = "IO error while saving file: " + ioEx.Message;
            }
            catch (Exception ex)
            {
                error = "Error while saving file: " + ex.Message;
            }

            return false;
        }

        public static bool SaveImage(RenderTargetBitmap bmp, string path, bool png, out string error)
        {
            error = null;

            if (bmp == null)
            {
                error = "No video frame available to save as image.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Destination path is invalid.";
                return false;
            }

            try
            {
                BitmapEncoder encoder = png ? new PngBitmapEncoder() : new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                using (var fs = File.OpenWrite(path))
                {
                    encoder.Save(fs);
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Access denied. Cannot save the image.";
            }
            catch (IOException ioEx)
            {
                error = "IO error while saving image: " + ioEx.Message;
            }
            catch (Exception ex)
            {
                error = "Error while saving image: " + ex.Message;
            }

            return false;
        }
    }
}


