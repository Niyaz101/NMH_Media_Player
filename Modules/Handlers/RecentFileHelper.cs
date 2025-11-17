using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace NMH_Media_Player.Modules.Helpers
{
    public static class RecentFileHelper
    {
        //  Safe AppData path for recent files
        private static readonly string RecentFilesPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NMH_Media_Player", "recent.txt");

        // ------------------------ Add a file to recent list ------------------------
        public static void AddRecentFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return;

                // Ensure app folder exists
                Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath));

                var recentFiles = new List<string>();

                if (File.Exists(RecentFilesPath))
                    recentFiles = File.ReadAllLines(RecentFilesPath).ToList();

                // Remove duplicates and insert latest on top
                recentFiles.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));
                recentFiles.Add(filePath);

                // Keep only last 10 files
                if (recentFiles.Count > 10)
                    recentFiles = recentFiles.Skip(recentFiles.Count - 10).ToList();

                File.WriteAllLines(RecentFilesPath, recentFiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving recent file list:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------------------------ Load recent files ------------------------
        public static List<string> GetRecentFiles(int maxCount = 10)
        {
            try
            {
                if (!File.Exists(RecentFilesPath))
                    return new List<string>();

                var files = File.ReadAllLines(RecentFilesPath)
                                .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                                .Reverse()
                                .Take(maxCount)
                                .ToList();

                return files;
            }
            catch
            {
                return new List<string>();
            }
        }

        // ------------------------ Clear all recent files ------------------------
        public static void ClearRecentFiles()
        {
            try
            {
                if (File.Exists(RecentFilesPath))
                    File.Delete(RecentFilesPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing recent files:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
