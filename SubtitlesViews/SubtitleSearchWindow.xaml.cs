using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NMH_Media_Player.SubtitlesViews
{
    public partial class SubtitleSearchWindow : Window
    {
        public class SubtitleItem
        {
            public string Name { get; set; }
            public string FileId { get; set; }
        }

        // Use ObservableCollection to keep ListBox in sync automatically
        private readonly ObservableCollection<SubtitleItem> searchResults = new ObservableCollection<SubtitleItem>();
        public string SelectedSubtitlePath { get; private set; }

        private readonly string ApiKey = "VRw4L76Ujn8C3EOFu0bbvICHF7u5wR7W"; // Replace with your API key
        private readonly string UserAgent = "NMHMediaPlayer/1.0";

        public SubtitleSearchWindow()
        {
            InitializeComponent();

            lstResults.ItemsSource = searchResults; // Bind once
            BtnDownload.IsEnabled = false;
            lstResults.SelectionChanged += LstResults_SelectionChanged;

            txtSearch.Text = "Enter movie or video name";
            txtSearch.Foreground = Brushes.Gray;
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
        }

        private void LstResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnDownload.IsEnabled = lstResults.SelectedItem != null;
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string query = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query) || query == "Enter movie or video name")
            {
                MessageBox.Show("Enter a movie or video name to search.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BtnDownload.IsEnabled = false;
            searchResults.Clear();

            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Api-Key", ApiKey);
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                string url = $"https://api.opensubtitles.com/api/v1/subtitles?query={Uri.EscapeDataString(query)}&languages=en";
                string json = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);
                foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    var attributes = item.GetProperty("attributes");

                    if (attributes.TryGetProperty("files", out var filesProp) && filesProp.GetArrayLength() > 0)
                    {
                        var firstFile = filesProp[0];
                        string fileName = firstFile.TryGetProperty("file_name", out var fileNameProp)
                                          ? fileNameProp.GetString() ?? "Subtitle"
                                          : "Subtitle";

                        string fileId = firstFile.TryGetProperty("file_id", out var fileIdProp)
                                        ? fileIdProp.GetRawText()
                                        : "";

                        if (!string.IsNullOrEmpty(fileId))
                        {
                            searchResults.Add(new SubtitleItem
                            {
                                Name = fileName,
                                FileId = fileId
                            });
                        }
                    }
                }

                if (searchResults.Count == 0)
                    MessageBox.Show("No subtitles found for your search.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (lstResults.SelectedItem is not SubtitleItem selected || string.IsNullOrEmpty(selected.FileId))
            {
                MessageBox.Show("Select a subtitle from the list.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Subtitle Files (*.srt)|*.srt|All Files (*.*)|*.*",
                FileName = selected.Name
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            string savePath = saveFileDialog.FileName;

            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("VRw4L76Ujn8C3EOFu0bbvICHF7u5wR7W", ApiKey);
                client.DefaultRequestHeaders.Add("NMHMediaPlayer/1.0", UserAgent);

                var requestContent = new StringContent($"{{\"file_id\":{selected.FileId}}}", System.Text.Encoding.UTF8, "application/json");
                string downloadUrl = "https://api.opensubtitles.com/api/v1/download";

                HttpResponseMessage response = null;
                int retries = 3;
                for (int i = 0; i < retries; i++)
                {
                    response = await client.PostAsync(downloadUrl, requestContent);
                    if (response.IsSuccessStatusCode) break;

                    if ((int)response.StatusCode == 503) await Task.Delay(2000);
                    else response.EnsureSuccessStatusCode();
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    MessageBox.Show("OpenSubtitles server is temporarily unavailable. Try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                string fileUrl = doc.RootElement.GetProperty("link").GetString() ?? "";

                if (string.IsNullOrEmpty(fileUrl))
                {
                    MessageBox.Show("Failed to get subtitle download link.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] data = await client.GetByteArrayAsync(fileUrl);

                if (Path.GetExtension(fileUrl).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string tempZip = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
                    await File.WriteAllBytesAsync(tempZip, data);

                    using ZipArchive archive = ZipFile.OpenRead(tempZip);
                    var srtEntry = archive.Entries[0];
                    using Stream entryStream = srtEntry.Open();
                    using FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                    await entryStream.CopyToAsync(fs);

                    File.Delete(tempZip);
                }
                else
                {
                    await File.WriteAllBytesAsync(savePath, data);
                }

                SelectedSubtitlePath = savePath;
                MessageBox.Show($"Subtitle downloaded and saved:\n{savePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download subtitle:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Enter movie or video name")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = Brushes.Black;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Enter movie or video name";
                txtSearch.Foreground = Brushes.Gray;
            }
        }
    }
}
