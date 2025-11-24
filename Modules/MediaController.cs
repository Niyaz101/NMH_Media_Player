using NMH.VideoFilter;
using NMH_Media_Player.Handlers;
using NMH_Media_Player.Modules.Helpers;
using NMH_Media_Player.Playback;
using NMH_Media_Player.Subtitles;
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

     
        private int currentIndex = -1;
        private bool isShuffle = false;
        private readonly Random random = new();

        public CancellationTokenSource _analysisCts;

        private List<string> playlist = new List<string>();
        public List<SubtitleEntry> SubtitleEntries { get; private set; } = new List<SubtitleEntry>();



        private readonly PlaybackSettingsManager playbackSettings = PlaybackSettingsManager.Instance;



        private NoMediaBackgroundGrid noMediaBackground;

        public string LastFilePath { get; set; } = string.Empty;
        public TimeSpan LastPosition { get; set; } = TimeSpan.Zero;
        public string CurrentFile => GetCurrentFile() ?? string.Empty;
        public bool IsShuffle => isShuffle;
        private bool resumePrompted = false;
        private static readonly string ResumeFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "NMH_Media_Player", "resume.txt");

       

       
        // ========================= Embedded Subtitles =========================
        public List<EmbeddedSubtitleTrack> EmbeddedSubtitleTracks { get; private set; } = new();
        public int CurrentEmbeddedSubtitleIndex { get; set; } = -1;  // Changed to public set
        public bool UseEmbeddedSubtitles { get; set; } = false;      // Changed to public set
        public EmbeddedSubtitleTrack? ActiveEmbeddedSubtitle { get; private set; }

        //========================== Temp Files Management for embdeded Subtitles ==================
        private readonly List<string> _tempSubtitleFiles = new List<string>();
        private static readonly string _tempFilePrefix = "embedded_";


        // ========================= External Subtitles =========================
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

        

            // Apply initial playback settings
            ApplyPlaybackSettings();

            // Subscribe to runtime changes
            playbackSettings.SettingsChanged += PlaybackSettings_SettingsChanged;

            



        }

        // ========================= Playlist Management =========================
        public void SetPlaylist(List<string> files)
        {
            playlist = files ?? new List<string>();
            currentIndex = playlist.Count > 0 ? 0 : -1;

            // Update title even if playback hasn't started yet
            UpdateWindowTitle();
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

                // 🧹 Clean up temp files when stopping
                CleanupCurrentTempFile();

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
        //public void Next(bool shuffleOverride = false)
        //{
        //    try
        //    {
        //        if (playlist.Count == 0)
        //        {
        //            ViewMenuHandler.ShowToast(mainWindow, "No video in the playlist!");
        //            return;
        //        }

        //        currentIndex = (shuffleOverride || isShuffle)
        //            ? random.Next(playlist.Count)
        //            : (currentIndex + 1) % playlist.Count;

        //        PlayCurrent();
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewMenuHandler.ShowToast(mainWindow, $"Failed to go to next video: {ex.Message}");
        //    }
        //}

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

                resumePrompted = false; // Reset flag for the new video
                PlayCurrent(startPlayback: true);
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
                 : (currentIndex - 1 + playlist.Count) % playlist.Count;


                resumePrompted = false; // Reset flag for the new video
                PlayCurrent(startPlayback: true);
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


        public void PlayCurrent(bool resumeFromLastPosition = true, bool userAction = false, bool startPlayback = true)
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
                    if (userAction)
                        ViewMenuHandler.ShowToast(mainWindow, "Playlist is empty!");
                    return;
                }

                if (!File.Exists(file))
                {
                    ViewMenuHandler.ShowToast(mainWindow, $"File not found: {file}");
                    return;
                }
                if (currentIndex < 0 || currentIndex >= playlist.Count)
                {
                    if (userAction)
                        ViewMenuHandler.ShowToast(mainWindow, "No video selected!");
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

                // ✅ START PLAYBACK IMMEDIATELY (moved up)
                UpdateWindowTitle();

                // Apply playback speed
                mediaPlayer.SpeedRatio = playbackSettings.PlaybackSpeed;

                // Resume logic
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

                // Start playback if requested
                if (startPlayback)
                {
                    mediaPlayer.Play();
                    timer.Start();
                }

                UpdateBackgroundVisibility(); // hide "No Media" text

                // Save recent file
                RecentFileHelper.AddRecentFile(file);

                // ------------------- Load Embedded Subtitles ASYNC (non-blocking) -------------------
                Debug.WriteLine("🚀 Starting async subtitle detection...");

                // Run subtitle detection in background without blocking playback
                Task.Run(async () =>
                {
                    try
                    {
                        // Small delay to let video start playing first
                        await Task.Delay(500);

                        if (token.IsCancellationRequested) return;

                        // Detect embedded subtitles
                        var tracks = EmbeddedSubtitleDetector.GetEmbeddedSubtitles(file);

                        if (token.IsCancellationRequested) return;

                        if (tracks != null && tracks.Count > 0)
                        {
                            // Update on UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (token.IsCancellationRequested) return;

                                EmbeddedSubtitleTracks.Clear();
                                EmbeddedSubtitleTracks.AddRange(tracks);

                                // Auto-load the preferred track
                                var preferredTrack = tracks.FirstOrDefault(t => t.Language.Equals("English", StringComparison.OrdinalIgnoreCase))
                                                  ?? tracks.FirstOrDefault(t => t.IsDefault)
                                                  ?? tracks.First();

                                LoadEmbeddedSubtitleAsExternal(preferredTrack.TrackIndex);

                                // Update menu
                                mainWindow?.UpdateEmbeddedSubtitlesMenu();

                                Debug.WriteLine($"✅ Auto-loaded embedded subtitle: {preferredTrack.Language}");
                            });
                        }
                        else
                        {
                            // No embedded subtitles found, try external subtitle
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (token.IsCancellationRequested) return;

                                string subtitlePath = Path.ChangeExtension(file, ".srt");
                                if (File.Exists(subtitlePath))
                                {
                                    LoadSubtitles(subtitlePath);
                                    Debug.WriteLine($"✅ Loaded external subtitle: {subtitlePath}");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"💥 Background subtitle detection failed: {ex.Message}");
                    }
                }, token);
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






        



        public void LoadSubtitles(string filePath)
        {
            try
            {
                SubtitleManager manager = new SubtitleManager();
                SubtitleEntries = manager.Load(filePath);
                ViewMenuHandler.ShowToast(mainWindow, $"Loaded {SubtitleEntries.Count} subtitle lines.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load subtitles:\n{ex.Message}");
            }
        }

        
        public void ClearSubtitles()
        {
            // 🧹 Clean up temp file when clearing subtitles
            CleanupCurrentTempFile();

            SubtitleEntries.Clear();
            CurrentSubtitleFile = null;
            subtitleTextBlock.Text = string.Empty;
        }

        public void UpdateSubtitle(TimeSpan currentTime)
        {
            if (SubtitleEntries == null || SubtitleEntries.Count == 0)
            {
                subtitleTextBlock.Text = "";
                return;
            }

            var entry = SubtitleEntries.FirstOrDefault(s =>
                currentTime >= s.StartTime && currentTime <= s.EndTime);

            subtitleTextBlock.Text = entry?.Text ?? "";
        }






        //private void SubtitleTimer_Tick(object sender, EventArgs e)
        //{
        //    if (SubtitleEntries == null || SubtitleEntries.Count == 0 || mediaPlayer.Source == null)
        //    {
        //        subtitleTextBlock.Text = "";
        //        return;
        //    }

        //    TimeSpan currentTime = mediaPlayer.Position;

        //    var currentSubtitle = SubtitleEntries.FirstOrDefault(s =>
        //        currentTime >= s.StartTime && currentTime <= s.EndTime);

        //    subtitleTextBlock.Text = currentSubtitle?.Text ?? "";
        //}

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



        private void UpdateBackgroundVisibility()
        {
            if (noMediaBackground == null) return;

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






        //private void ApplyPlaybackSettings()
        //{
        //    if (mediaPlayer == null || playbackSettings == null) return;

        //    // Playback speed
        //    mediaPlayer.SpeedRatio = playbackSettings.PlaybackSpeed;

        //    // Remove existing MediaEnded subscription to avoid duplicates
        //    mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;

        //    // Subscribe if looping or auto-next is enabled
        //    if (playbackSettings.LoopSingle || playbackSettings.AutoPlayNext)
        //    {
        //        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        //    }
        //}


        private void ApplyPlaybackSettings()
        {
            if (mediaPlayer == null || playbackSettings == null) return;

            // Set playback speed safely
            try
            {
                mediaPlayer.SpeedRatio = playbackSettings.PlaybackSpeed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting playback speed: {ex.Message}");
            }

            // Remove previous subscription to prevent duplicates
            mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;

            // Subscribe if looping or autoplay next is enabled
            if (playbackSettings.LoopSingle || playbackSettings.AutoPlayNext)
            {
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            }
        }



        private void PlaybackSettings_SettingsChanged(object? sender, EventArgs e)
        {
            // This runs on UI thread
            Application.Current.Dispatcher.Invoke(() => ApplyPlaybackSettings());
        }



        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Just forward the event to your static handler
            MediaPlayerEvents.MediaEnded(sender, e);
        }












        public void ApplyHardwareAcceleration(bool enabled) => PlaybackSettingsManager.Instance.EnableHardwareAcceleration = enabled;
        public void ApplyMultiThreadedDecoding(bool enabled) => PlaybackSettingsManager.Instance.EnableMultiThreadedDecoding = enabled;
        public void ApplyCustomRenderPipeline(bool enabled) => PlaybackSettingsManager.Instance.UseCustomRenderPipeline = enabled;
        public void ApplyBetaFeatures(bool enabled) => PlaybackSettingsManager.Instance.EnableBetaFeatures = enabled;






        private void UpdateWindowTitle()
        {
            if (mainWindow == null) return;

            string currentFile = CurrentFile;
           

            string displayTitle = string.IsNullOrEmpty(currentFile)
            ? "NMH Media Player"
            : $"NMH Media Player - {Path.GetFileName(currentFile)}";

            mainWindow.SetCustomTitle(displayTitle);

        }




        // ========================= Embedded Subtitles Loading =========================
        // ========================= Embedded Subtitles =========================


        // Auto-detect and load embedded subtitles
        // Auto-detect and load embedded subtitles
        // Auto-detect and load embedded subtitles
        // Make this method async
        public async Task AutoLoadEmbeddedSubtitlesAsync(string videoFilePath)
        {
            try
            {
                Debug.WriteLine($"=== AUTO-LOAD EMBEDDED SUBTITLES STARTED ===");

                // Clean up any previous temp file
                CleanupCurrentTempFile();

                EmbeddedSubtitleTracks.Clear();

                if (!File.Exists(videoFilePath))
                {
                    Debug.WriteLine("❌ Video file doesn't exist");
                    return;
                }

                // 1. FIRST: Look in video if there is embedded subtitle (async)
                Debug.WriteLine("🔍 Detecting embedded subtitles asynchronously...");

                // Run detection on background thread to avoid blocking UI
                var tracks = await Task.Run(() => EmbeddedSubtitleDetector.GetEmbeddedSubtitles(videoFilePath));

                if (tracks == null || tracks.Count == 0)
                {
                    Debug.WriteLine("❌ No embedded subtitles found");
                    return;
                }

                Debug.WriteLine($"✅ Found {tracks.Count} embedded subtitle tracks");
                EmbeddedSubtitleTracks.AddRange(tracks);

                // 2. IF YES: Extract it and save in temp (also async)
                var preferredTrack = tracks.FirstOrDefault(t => t.Language.Equals("English", StringComparison.OrdinalIgnoreCase))
                                  ?? tracks.FirstOrDefault(t => t.IsDefault)
                                  ?? tracks.First();

                Debug.WriteLine($"🎯 Selected track: {preferredTrack.Name} (Language: {preferredTrack.Language}, Default: {preferredTrack.IsDefault})");

                // Run extraction on background thread
                string tempSubtitlePath = await Task.Run(() => ExtractEmbeddedSubtitleToFile(videoFilePath, preferredTrack.TrackIndex));

                if (!string.IsNullOrEmpty(tempSubtitlePath) && File.Exists(tempSubtitlePath))
                {
                    Debug.WriteLine($"✅ Extracted to: {tempSubtitlePath}");

                    // 3. THEN: From temp detect it as external subtitle and auto load
                    // This part needs to run on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Debug.WriteLine("📥 Loading extracted subtitle...");
                        LoadSubtitles(tempSubtitlePath);
                        CurrentSubtitleFile = tempSubtitlePath;

                        ViewMenuHandler.ShowToast(mainWindow, $"Auto-loaded: {preferredTrack.Language} ({preferredTrack.Name})");
                        Debug.WriteLine($"✅ Embedded subtitle loaded successfully");
                    });
                }
                else
                {
                    Debug.WriteLine("❌ Failed to extract embedded subtitle");
                    // Don't show toast here to avoid UI blocking
                }

                Debug.WriteLine($"=== AUTO-LOAD EMBEDDED SUBTITLES COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Auto-load embedded subtitles failed: {ex.Message}");
            }
        }
        private string ExtractEmbeddedSubtitleToFile(string mediaFilePath, int trackIndex)
        {
            try
            {
                Debug.WriteLine($"🔄 Extracting subtitle track {trackIndex} from: {mediaFilePath}");

                string tempPath = Path.GetTempFileName();
                tempPath = Path.ChangeExtension(tempPath, ".srt");
                Debug.WriteLine($"📝 Temp file: {tempPath}");

                // Use FFmpeg to extract the subtitle to a temporary file
                string ffmpegArgs = $"-i \"{mediaFilePath}\" -map 0:s:{trackIndex} \"{tempPath}\" -y";
                Debug.WriteLine($"🔧 FFmpeg command: ffmpeg {ffmpegArgs}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit(10000); // 10 second timeout

                    Debug.WriteLine($"FFmpeg exit code: {process.ExitCode}");

                    if (process.ExitCode == 0 && File.Exists(tempPath))
                    {
                        Debug.WriteLine($"✅ Extraction successful, file size: {new FileInfo(tempPath).Length} bytes");

                        // Track this temp file for cleanup
                        _tempSubtitleFiles.Add(tempPath);
                        Debug.WriteLine($"📁 Tracking temp file: {tempPath} (Total: {_tempSubtitleFiles.Count})");

                        return tempPath;
                    }
                    else
                    {
                        Debug.WriteLine($"❌ Extraction failed or file doesn't exist");
                        // Clean up failed extraction
                        TryDeleteFile(tempPath);
                        return null;
                    }
                }
                else
                {
                    Debug.WriteLine($"❌ Failed to start FFmpeg process");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Extraction failed: {ex.Message}");
                return null;
            }
        }

        // Manual method for menu selection
        public void LoadEmbeddedSubtitleAsExternal(int trackIndex)
        {
            try
            {
                var track = EmbeddedSubtitleTracks.FirstOrDefault(t => t.TrackIndex == trackIndex);
                if (track == null) return;

                string videoFile = CurrentFile;
                if (string.IsNullOrEmpty(videoFile)) return;

                // Clean up previous temp file before loading new one
                CleanupCurrentTempFile();

                string tempSubtitlePath = ExtractEmbeddedSubtitleToFile(videoFile, trackIndex);

                if (!string.IsNullOrEmpty(tempSubtitlePath) && File.Exists(tempSubtitlePath))
                {
                    LoadSubtitles(tempSubtitlePath);
                    CurrentSubtitleFile = tempSubtitlePath;
                    ViewMenuHandler.ShowToast(mainWindow, $"Loaded embedded subtitle: {track.Name}");
                }
            }
            catch (Exception ex)
            {
                ViewMenuHandler.ShowToast(mainWindow, $"Failed to load embedded subtitle: {ex.Message}");
            }
        }

        private bool IsFFmpegAvailable()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(3000);
                    bool available = process.ExitCode == 0;
                    Debug.WriteLine($"FFmpeg available: {available}");
                    return available;
                }
                return false;
            }
            catch
            {
                Debug.WriteLine("❌ FFmpeg not found in PATH");
                return false;
            }
        }

        // Clean up all temporary subtitle files
        public void CleanupTempFiles()
        {
            try
            {
                Debug.WriteLine($"🧹 Cleaning up {_tempSubtitleFiles.Count} temporary subtitle files...");

                int deletedCount = 0;
                foreach (string tempFile in _tempSubtitleFiles.ToList()) // Use ToList to avoid modification during iteration
                {
                    if (TryDeleteFile(tempFile))
                    {
                        _tempSubtitleFiles.Remove(tempFile);
                        deletedCount++;
                        Debug.WriteLine($"✅ Deleted: {tempFile}");
                    }
                }

                Debug.WriteLine($"🧹 Cleanup completed: {deletedCount} files deleted, {_tempSubtitleFiles.Count} remaining in list");

                // Also clean up any orphaned temp files from previous sessions
                CleanupOrphanedTempFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Temp file cleanup failed: {ex.Message}");
            }
        }

        // Clean up orphaned temp files from previous sessions
        private void CleanupOrphanedTempFiles()
        {
            try
            {
                string tempDir = Path.GetTempPath();
                var orphanedFiles = Directory.GetFiles(tempDir, $"{_tempFilePrefix}*.srt")
                                           .Concat(Directory.GetFiles(tempDir, "*.srt"))
                                           .Where(f => !_tempSubtitleFiles.Contains(f))
                                           .ToList();

                Debug.WriteLine($"🔍 Found {orphanedFiles.Count} orphaned subtitle files to clean up");

                int deletedCount = 0;
                foreach (string orphanedFile in orphanedFiles)
                {
                    // Only delete files that are likely from our app (recent files)
                    var fileInfo = new FileInfo(orphanedFile);
                    if (fileInfo.CreationTime > DateTime.Now.AddHours(-24)) // Delete files from last 24 hours
                    {
                        if (TryDeleteFile(orphanedFile))
                        {
                            deletedCount++;
                            Debug.WriteLine($"✅ Deleted orphaned: {Path.GetFileName(orphanedFile)}");
                        }
                    }
                }

                Debug.WriteLine($"🧹 Orphaned cleanup: {deletedCount} files deleted");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Orphaned file cleanup failed: {ex.Message}");
            }
        }

        // Safe file deletion with error handling
        private bool TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Could not delete file {filePath}: {ex.Message}");
                return false;
            }
        }

        // Clean up current temp file when switching subtitles
        public void CleanupCurrentTempFile()
        {
            if (!string.IsNullOrEmpty(CurrentSubtitleFile) &&
                CurrentSubtitleFile.Contains(Path.GetTempPath()) &&
                File.Exists(CurrentSubtitleFile))
            {
                Debug.WriteLine($"🔄 Cleaning up current temp subtitle: {CurrentSubtitleFile}");
                TryDeleteFile(CurrentSubtitleFile);

                // Remove from tracking list
                _tempSubtitleFiles.Remove(CurrentSubtitleFile);
                CurrentSubtitleFile = null;
            }
        }


        // Add this method to debug what's being detected
        public void DebugDetectedSubtitles()
        {
            try
            {
                Debug.WriteLine("=== DEBUG DETECTED SUBTITLES ===");
                foreach (var track in EmbeddedSubtitleTracks)
                {
                    Debug.WriteLine($"Track {track.TrackIndex}: Language='{track.Language}', Name='{track.Name}', Codec='{track.Codec}', Default={track.IsDefault}");
                }
                Debug.WriteLine("=== END DEBUG ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Debug failed: {ex.Message}");
            }
        }


    }

}



