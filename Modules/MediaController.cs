using NMH.VideoFilter;
using NMH_Media_Player.Handlers;
using NMH_Media_Player.Modules.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static TorchSharp.torch.jit;
using static TorchSharp.torch.optim.lr_scheduler.impl.CyclicLR;

namespace NMH_Media_Player.Modules
{
    public class MediaController
    {
        private readonly MediaElement mediaPlayer;
        private readonly DispatcherTimer timer;
        public MediaElement Player => mediaPlayer;
        private readonly TextBlock subtitleTextBlock;
        private readonly MainWindow mainWindow;

        private List<string> playlist = [];
        private int currentIndex = -1;
        private bool isShuffle = false;
        private readonly Random random = new();

        public CancellationTokenSource _analysisCts;




        private NoMediaBackgroundGrid noMediaBackground;

        public string LastFilePath { get; set; } = string.Empty;
        public TimeSpan LastPosition { get; set; } = TimeSpan.Zero;
        public string CurrentFile => GetCurrentFile() ?? string.Empty;
        public bool IsShuffle => isShuffle;
        private bool resumePrompted = false;
        private static readonly string ResumeFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "NMH_Media_Player", "resume.txt");

        // ========================= Subtitles =========================
        public List<SubtitleEntry> SubtitleEntries { get; private set; } = [];
        public string? CurrentSubtitleFile { get; private set; } = null;

        // ✅ Use the MainWindow's timer instead of creating a new one
        public MediaController(MediaElement player, TextBlock subtitleBlock, MainWindow window, DispatcherTimer sharedTimer)
        {
            mediaPlayer = player ?? throw new ArgumentNullException(nameof(player));
            subtitleTextBlock = subtitleBlock ?? throw new ArgumentNullException(nameof(subtitleBlock));
            mainWindow = window ?? throw new ArgumentNullException(nameof(window));
            timer = sharedTimer ?? throw new ArgumentNullException(nameof(sharedTimer));

            // Attach only the subtitle update tick to the shared timer
            timer.Tick += SubtitleTimer_Tick;
        }

        // ========================= Playlist Management =========================
        public void SetPlaylist(List<string> files)
        {
            playlist = files ?? new List<string>();
            currentIndex = playlist.Count > 0 ? 0 : -1;
        }

        public void AddToPlaylist(string file)
        {
            if (!string.IsNullOrEmpty(file) && !playlist.Contains(file))
            {
                playlist.Add(file);
                if (currentIndex == -1) currentIndex = 0;
            }
        }


        public List<string> GetPlaylist()
        {
            return playlist.ToList(); // return a copy
        }

 



        // ========================= Playback Controls =========================
        public void Play()
        {
            try
            {
                if (mediaPlayer.Source == null)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video is currently playing!");
                    return;
                }

                // If media is at the end, reset to beginning
                if (mediaPlayer.NaturalDuration.HasTimeSpan &&
                    mediaPlayer.Position >= mediaPlayer.NaturalDuration.TimeSpan)
                {
                    mediaPlayer.Position = TimeSpan.Zero;
                }

                mediaPlayer.Play();
                timer.Start();

                UpdateBackgroundVisibility(); // ✅ Hide "No Media" text
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to play video: {ex.Message}");
            }
        }

        public void Pause()
        {
            try
            {
                if (mediaPlayer.Source == null)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video is currently playing!");
                    return;
                }

                if (mediaPlayer.CanPause)
                {
                    mediaPlayer.Pause();
                    timer.Stop();
                }
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to pause video: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                if (mediaPlayer.Source == null)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video is currently playing!");
                    return;
                }

                LastFilePath = CurrentFile;
                LastPosition = mediaPlayer.Position;

                mediaPlayer.Stop();
                timer.Stop();

                mediaPlayer.Position = TimeSpan.Zero;
                subtitleTextBlock.Text = string.Empty;

                mainWindow.progressSlider.Value = 0;
                mainWindow.lblDuration.Content = "00:00:00 / 00:00:00";

                UpdateBackgroundVisibility(); // ✅ Show "No Media" text if stopped
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to stop video: {ex.Message}");
            }
        }

        // ========================= Navigation =========================
        public void Next(bool shuffleOverride = false)
        {
            try
            {
                if (playlist.Count == 0)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video in the playlist!");
                    return;
                }

                currentIndex = (shuffleOverride || isShuffle)
                    ? random.Next(playlist.Count)
                    : (currentIndex + 1) % playlist.Count;

                PlayCurrent();
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to go to next video: {ex.Message}");
            }
        }

        public void Previous(bool shuffleOverride = false)
        {
            try
            {
                if (playlist.Count == 0)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video in the playlist!");
                    return;
                }

                currentIndex = (shuffleOverride || isShuffle)
                    ? random.Next(playlist.Count)
                    : currentIndex - 1;

                if (currentIndex < 0) currentIndex = playlist.Count - 1;

                PlayCurrent();
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to go to previous video: {ex.Message}");
            }
        }

        // ========================= Skip / Seek =========================
        public void Forward(int seconds = 10)
        {
            try
            {
                if (mediaPlayer.Source == null || !mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video is currently playing!");
                    return;
                }

                TimeSpan newPosition = mediaPlayer.Position + TimeSpan.FromSeconds(seconds);
                if (newPosition > mediaPlayer.NaturalDuration.TimeSpan)
                    newPosition = mediaPlayer.NaturalDuration.TimeSpan;

                mediaPlayer.Position = newPosition;
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to skip forward: {ex.Message}");
            }
        }


        public void Rewind(int seconds = 10)
        {
            try
            {
                if (mediaPlayer.Source == null)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video is currently playing!");
                    return;
                }

                TimeSpan newPosition = mediaPlayer.Position - TimeSpan.FromSeconds(seconds);
                if (newPosition < TimeSpan.Zero) newPosition = TimeSpan.Zero;

                mediaPlayer.Position = newPosition;
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to rewind: {ex.Message}");
            }
        }

        // ========================= Play Current =========================


        public void PlayCurrent(bool resumeFromLastPosition = true, bool userAction = false)
        {
            try
            {
                if (playlist == null || playlist.Count == 0)
                {
                    if (userAction)
                        ViewMenuHandler.ShowToast(mainWindow, "Playlist is empty!");
                    return;
                }

                if (currentIndex < 0 || currentIndex >= playlist.Count)
                {
                    if (userAction)
                        ViewMenuHandler.ShowToast(mainWindow, "No video selected!");
                    return;
                }

                string file = playlist[currentIndex];
                if (!File.Exists(file))
                {
                    if (userAction)
                        ViewMenuHandler.ShowToast(mainWindow, $"File not found: {file}");
                    return;
                }

                // Clear previous subtitles
                ClearSubtitles();

                // Reset UI
                if (mainWindow != null)
                {
                    mainWindow.progressSlider.Value = 0;
                    mainWindow.lblDuration.Content = "00:00:00 / 00:00:00";
                }

                mediaPlayer.Source = new Uri(file);

                // Resume logic only once per session
                if (resumeFromLastPosition && !resumePrompted && file == LastFilePath && LastPosition > TimeSpan.Zero)
                {
                    var result = MessageBox.Show(
                        $"Do you want to resume '{Path.GetFileName(file)}' from {LastPosition:hh\\:mm\\:ss}?",
                        "Resume Playback", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    mediaPlayer.Position = result == MessageBoxResult.Yes ? LastPosition : TimeSpan.Zero;
                    resumePrompted = true;
                }
                else
                {
                    mediaPlayer.Position = TimeSpan.Zero;
                }

                mediaPlayer.Play();
                timer.Start();


                UpdateBackgroundVisibility(); //   hide "No Media" text if stopped

                RecentFileHelper.AddRecentFile(file);

                // Auto-load subtitles
                string subtitlePath = Path.ChangeExtension(file, ".srt");
                if (File.Exists(subtitlePath))
                    LoadSubtitles(subtitlePath);
            }
            catch (Exception ex)
            {
                if (userAction)
                    ViewMenuHandler.ShowToast(mainWindow, $"Failed to play video: {ex.Message}");
            }
        }



        public string? GetCurrentFile()
        {
            if (currentIndex < 0 || currentIndex >= playlist.Count) return null;
            return playlist[currentIndex];
        }



        public void ToggleShuffle()
        {
            try
            {
                isShuffle = !isShuffle;
                ViewMenuHandler.ShowToast(mainWindow, $"Shuffle is now {(isShuffle ? "ON" : "OFF")}");
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to toggle shuffle: {ex.Message}");
            }
        }




        // ========================= Session Management =========================
        public void SaveLastSession()
        {
            if (!string.IsNullOrEmpty(CurrentFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResumeFilePath));
                File.WriteAllText(ResumeFilePath, $"{CurrentFile}|{mediaPlayer.Position.TotalSeconds}");
            }
        }


        public void LoadLastSession()
        {
            if (File.Exists(ResumeFilePath))
            {
                string[] data = File.ReadAllText(ResumeFilePath).Split('|');
                if (data.Length == 2)
                {
                    LastFilePath = data[0];
                    if (double.TryParse(data[1], out double seconds))
                        LastPosition = TimeSpan.FromSeconds(seconds);
                }
            }
        }


        // ========================= Utility =========================
        public bool IsAudioFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            string ext = Path.GetExtension(path).ToLowerInvariant();

            // Supported audio formats
            string[] SupportedAudioExtensions =
            {
        ".mp3", ".aac", ".m4a", ".wma", ".ogg", ".opus", ".amr", ".ra",
        ".flac", ".alac", ".ape", ".wv", ".tta",
        ".wav", ".aiff", ".aif", ".pcm",
        ".ac3", ".dts", ".mka",
        ".voc", ".au", ".snd", ".caf",
        ".midi", ".mid", ".kar", ".spx"
    };

            return SupportedAudioExtensions.Contains(ext);
        }
        public bool IsVideoFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string[] SupportedVideoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v" };
            return SupportedVideoExtensions.Contains(ext);
        }




        //--------------------------------------- Used for Forawarding and Rewinding the media ----------------------------
        // used in the NavigationEvents.cs File

        // Skip forward
        //public void Forward(int seconds = 10)
        //{
        //    if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
        //    {
        //        TimeSpan newPosition = mediaPlayer.Position + TimeSpan.FromSeconds(seconds);
        //        if (newPosition > mediaPlayer.NaturalDuration.TimeSpan)
        //            newPosition = mediaPlayer.NaturalDuration.TimeSpan;

        //        mediaPlayer.Position = newPosition;
        //    }
        //}

        //// Skip backward
        //public void Rewind(int seconds = 10)
        //{
        //    if (mediaPlayer.Source != null)
        //    {
        //        TimeSpan newPosition = mediaPlayer.Position - TimeSpan.FromSeconds(seconds);
        //        if (newPosition < TimeSpan.Zero)
        //            newPosition = TimeSpan.Zero;

        //        mediaPlayer.Position = newPosition;
        //    }
        //}




        //----------------------------------------- Subtitles Management ---------------------------------------------

        // Shift all subtitle timestamps by given milliseconds
        public void ShiftSubtitles(int shiftMilliseconds)
        {
            if (SubtitleEntries == null || SubtitleEntries.Count == 0)
            {
                MessageBox.Show("No subtitles loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var subtitle in SubtitleEntries)
            {
                subtitle.StartTime = subtitle.StartTime.Add(TimeSpan.FromMilliseconds(shiftMilliseconds));
                subtitle.EndTime = subtitle.EndTime.Add(TimeSpan.FromMilliseconds(shiftMilliseconds));
            }

            // Optional: refresh subtitles display if you have a renderer
        }






        // Load subtitles from a .srt file
        public void LoadSubtitles(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Subtitle file not found:\n{filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                CurrentSubtitleFile = filePath;
                SubtitleEntries.Clear();

                string[] lines = File.ReadAllLines(filePath);
                SubtitleEntry? entry = null;
                var timeRegex = new Regex(@"(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})");

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (entry != null)
                        {
                            SubtitleEntries.Add(entry);
                            entry = null;
                        }
                        continue;
                    }

                    if (int.TryParse(line.Trim(), out int _)) // subtitle index line
                        continue;

                    var match = timeRegex.Match(line);
                    if (match.Success)
                    {
                        entry = new SubtitleEntry
                        {
                            StartTime = TimeSpan.ParseExact(match.Groups[1].Value, @"hh\:mm\:ss\,fff", null),
                            EndTime = TimeSpan.ParseExact(match.Groups[2].Value, @"hh\:mm\:ss\,fff", null)
                        };
                    }
                    else if (entry != null)
                    {
                        if (!string.IsNullOrEmpty(entry.Text))
                            entry.Text += "\n";
                        entry.Text += line.Trim();
                    }
                }

                if (entry != null)
                    SubtitleEntries.Add(entry);

                // Instead of MessageBox.Show
                ViewMenuHandler.ShowToast(mainWindow, $"Loaded {SubtitleEntries.Count} subtitle lines.");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load subtitles:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }








    


        private void SubtitleTimer_Tick(object sender, EventArgs e)
        {
            if (SubtitleEntries == null || SubtitleEntries.Count == 0 || mediaPlayer.Source == null)
            {
                subtitleTextBlock.Text = "";
                return;
            }

            TimeSpan currentTime = mediaPlayer.Position;

            var currentSubtitle = SubtitleEntries.FirstOrDefault(s =>
                currentTime >= s.StartTime && currentTime <= s.EndTime);

            subtitleTextBlock.Text = currentSubtitle?.Text ?? "";
        }




        // used for playing a specific file from the playlist
        public void PlayFromPlaylist(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            int index = playlist.IndexOf(filePath);
            if (index < 0)
            {
                playlist.Add(filePath);
                currentIndex = playlist.Count - 1;
            }
            else
            {
                currentIndex = index;
            }

            PlayCurrent(); // uses your main play logic
        }




        public void ClearSubtitles()
        {
            SubtitleEntries.Clear();
            CurrentSubtitleFile = null;
            subtitleTextBlock.Text = string.Empty;
        }






        public async Task PlayCurrentWithFilterRealtimeAsync(bool startPlayback = true)
        {
            try
            {
                string file = GetCurrentFile();
                if (string.IsNullOrEmpty(file)) return;

                // Stop any previous analysis first
                _analysisCts?.Cancel();
                _analysisCts = new CancellationTokenSource();
                var token = _analysisCts.Token;

                // ------------------- Playback Prep -------------------
                if (playlist == null || playlist.Count == 0)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "Playlist is empty!");
                    return;
                }

                if (currentIndex < 0 || currentIndex >= playlist.Count)
                {
                    ViewMenuHandler.ShowToast(mainWindow, "No video selected!");
                    return;
                }

                if (!File.Exists(file))
                {
                    ViewMenuHandler.ShowToast(mainWindow, $"File not found: {file}");
                    return;
                }

                // Clear previous subtitles
                ClearSubtitles();

                // Reset UI
                if (mainWindow != null)
                {
                    mainWindow.progressSlider.Value = 0;
                    mainWindow.lblDuration.Content = "00:00:00 / 00:00:00";
                }

                mediaPlayer.Source = new Uri(file);
                if (startPlayback)
                {
                    mediaPlayer.Play();
                    timer.Start();
                }

                // Resume logic only once per session
                if (file == LastFilePath && LastPosition > TimeSpan.Zero && !resumePrompted)
                {
                    var result = MessageBox.Show(
                        $"Do you want to resume '{Path.GetFileName(file)}' from {LastPosition:hh\\:mm\\:ss}?",
                        "Resume Playback", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    mediaPlayer.Position = result == MessageBoxResult.Yes ? LastPosition : TimeSpan.Zero;
                    resumePrompted = true;
                }
                else
                {
                    mediaPlayer.Position = TimeSpan.Zero;
                }

                // Start playback immediately
                mediaPlayer.Play();
                timer.Start();
                UpdateBackgroundVisibility(); // hide "No Media" text

                // ------------------- Save Recent & Load Subtitles -------------------
                RecentFileHelper.AddRecentFile(file);

                string subtitlePath = Path.ChangeExtension(file, ".srt");
                if (File.Exists(subtitlePath))
                    LoadSubtitles(subtitlePath);

                // ------------------- Skip filter for audio -------------------
                //if (IsAudioFile(file)) return;

                //if (!Filter.IsModelLoaded)
                //{
                //    MessageBox.Show(
                //        "⚠️ Unsafe content filter is not loaded. Analysis skipped.",
                //        "Filter Not Ready",
                //        MessageBoxButton.OK,
                //        MessageBoxImage.Warning);
                //    return;
                //}

                //bool blocked = false;

                //// Extract frames
                //await Filter.CaptureFramesAsync(file);

                //// Background analysis
                //_ = Task.Run(async () =>
                //{
                //    try
                //    {
                //        await Filter.AnalyzeFramesAsync(
                //            onUnsafeDetected: () =>
                //            {
                //                if (token.IsCancellationRequested) return;
                //                Application.Current.Dispatcher.Invoke(() =>
                //                {
                //                    if (blocked) return;
                //                    blocked = true;
                //                    MessageBox.Show(
                //                        "⚠️ Unsafe content detected!",
                //                        "Content Warning",
                //                        MessageBoxButton.OK,
                //                        MessageBoxImage.Warning);
                //                    Pause();
                //                });
                //            },
                //            skipForwardAsync: async () =>
                //            {
                //                if (token.IsCancellationRequested) return;
                //                await Application.Current.Dispatcher.InvokeAsync(() =>
                //                {
                //                    try
                //                    {
                //                        if (mediaPlayer == null) return;
                //                        var newPos = mediaPlayer.Position.TotalSeconds + 10;
                //                        if (newPos < mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds)
                //                            mediaPlayer.Position = TimeSpan.FromSeconds(newPos);
                //                        else
                //                            Pause();
                //                    }
                //                    catch (Exception innerEx)
                //                    {
                //                        Debug.WriteLine($"SkipForwardAsync error: {innerEx.Message}");
                //                    }
                //                });
                //            });
                //    }
                //    catch (OperationCanceledException)
                //    {
                //        Debug.WriteLine("Frame analysis cancelled.");
                //    }
                //    catch (Exception ex)
                //    {
                //        Debug.WriteLine($"AnalyzeFramesAsync error: {ex}");
                //    }
                //}, token);

            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("PlayCurrentWithFilterRealtimeAsync cancelled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlayCurrentWithFilterRealtimeAsync failed: {ex}");
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }






        private void UpdateBackgroundVisibility()
        {
            if (mediaPlayer.Source == null)
                noMediaBackground.Show();
            else
                noMediaBackground.Hide();
        }


        public void InitializeNoMediaBackground(Grid grid)
        {
            noMediaBackground = new NoMediaBackgroundGrid(grid);
            UpdateBackgroundVisibility();
        }



    }


}
