using ICSharpCode.SharpZipLib.Tar;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using NAudio.Gui;
using NMH.VideoFilter;
using NMH_Media_Player.ColorPicker;
using NMH_Media_Player.Handlers;
using NMH_Media_Player.Modules;
using NMH_Media_Player.Modules.Handlers;
using NMH_Media_Player.Modules.Helpers;
using NMH_Media_Player.Properties;
using NMH_Media_Player.SettingsWindow;
using NMH_Media_Player.Subtitles;
using NMH_Media_Player.Thumbnails;
using NMH_Media_Player.VideoScaling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace NMH_Media_Player
{
    public partial class MainWindow : Window
    {


        public bool IsFullScreen { get; internal set; }
        // Backup window size and position
        public double _restoreTop;
        public double _restoreLeft;    // these are used to restore the window size and position after exiting fullscreen in ViewMenuHandler.cs
        public double _restoreWidth;
        public double _restoreHeight;
        private const int edge = 5;
        //============================================


        //-------------------------------------- Fields & Timer --------------------------------------------------------
        private DispatcherTimer timer;
        private bool isDraggingSlider = false;
        private bool isClickSeeking = false;

        public MediaController mediaController { get; private set; }
        private AudioVisualizer visualizer;
        private int selectedVisualizerPreset = 0;

        private VideoScale _videoScale;



        

      


        //-------------------------------------- Constructor -----------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();



            // Resize handler for dynamic subtitle scaling
            VideoContainer.SizeChanged += VideoContainer_SizeChanged;




            // --- Restore Window Size & Position ---
            if (Settings.Default.StartMaximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            else if (Settings.Default.RememberWindow)
            {
                this.Width = Settings.Default.WindowWidth;
                this.Height = Settings.Default.WindowHeight;
                this.Left = Settings.Default.WindowLeft;
                this.Top = Settings.Default.WindowTop;
            }

            ApplyInterfaceSettings();
            ApplySubtitleSettings();  // APPLY SETTINGS AFTER InitializeComponent()



            string savedAccent = Settings.Default.AccentColor ?? "blue";
            ApplyAccentColor(savedAccent);













            //---------------------------------- NSFW Filter Initialization ----------------------------------
            string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
            string modelPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Models", "320n.torchscript"); // TorchScript file!
            string framesFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NMHMediaPlayer", "Frames");

            Filter.Initialize(ffmpegPath, modelPath, framesFolder);
            //---------------------------------- End Filter Initialization ----------------------------------

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += Timer_Tick;

           

            mediaController = new MediaController(mediaPlayer, SubtitleTextBlock, this, timer);

            mediaController.InitializeNoMediaBackground(PlayerBackgroundGrid);

            _videoScale = new VideoScale(mediaController, this);

            var volumeController = new VolumeController(mediaPlayer, volumeSlider, volumeLabel);


            MenuEvents.CurrentVideoPlayer = mediaPlayer;

            // Initialize Visualizer
            visualizer = new AudioVisualizer(VisualizerCanvas);
            VisualizerCanvas.SizeChanged += (s, e) => { /* AudioVisualizer restarts layout on next Start */ };

            // Set initial volume
            mediaPlayer.Volume = volumeSlider.Value;

            // Load previous session (resume)
            LoadLastSession();

            
            this.MouseMove += Window_MouseMove;

            //------------------------------------------- save color ------------------------------------------
            // Apply saved theme AFTER window is fully loaded
            Loaded += MainWindow_Loaded;

            InitializeKeyboardShortcuts();




        }

        private void InitializeKeyboardShortcuts()
        {
            // View Menu Shortcuts
            AddKeyBinding(Key.D0, ModifierKeys.Control, (s, e) => ViewMenuHandler.HideMenu_Click(null, null, this));
            AddKeyBinding(Key.D1, ModifierKeys.Control, (s, e) => ViewMenuHandler.SeekBar_Click(null, null, this));
            AddKeyBinding(Key.D2, ModifierKeys.Control, (s, e) => ViewMenuHandler.Controls_Click(null, null, this));
            AddKeyBinding(Key.D3, ModifierKeys.Control, (s, e) => ViewMenuHandler.Information_Click(null, null, this));
            AddKeyBinding(Key.D4, ModifierKeys.Control, (s, e) => ViewMenuHandler.Statistics_Click(null, null, this));
            AddKeyBinding(Key.D5, ModifierKeys.Control, (s, e) => ViewMenuHandler.Status_Click(null, null, this));
            AddKeyBinding(Key.S, ModifierKeys.Control, (s, e) => ViewMenuHandler.LoadSubtitle_Click(null, null, this));
            AddKeyBinding(Key.D6, ModifierKeys.Control, (s, e) => ViewMenuHandler.Subresync_Click(null, null, this));
            AddKeyBinding(Key.D7, ModifierKeys.Control, (s, e) => ViewMenuHandler.Playlist_Click(null, null, this));
            AddKeyBinding(Key.D8, ModifierKeys.Control, (s, e) => ViewMenuHandler.Capture_Click(null, null, this));
            AddKeyBinding(Key.F11, ModifierKeys.None, (s, e) => ViewMenuHandler.FullScreen_Click(null, null, this));


            // File Menu Shortcuts
            AddKeyBinding(Key.Q, ModifierKeys.Control, (s, e) => BtnOpen_Click(null, null));
            AddKeyBinding(Key.O, ModifierKeys.Control, (s, e) => OpenFileUrl_Click(null, null));
            AddKeyBinding(Key.C, ModifierKeys.Control, (s, e) => OpenDVD_Click(null, null));
            AddKeyBinding(Key.I, ModifierKeys.Control, (s, e) => OpenDevice_Click(null, null));
            AddKeyBinding(Key.D, ModifierKeys.Control, (s, e) => OpenDirectory_Click(null, null));
            AddKeyBinding(Key.K, ModifierKeys.Control, (s, e) => OpenDisc_Click(null, null));
            AddKeyBinding(Key.S, ModifierKeys.Alt, (s, e) => SaveCopy_Click(null, null));
            AddKeyBinding(Key.I, ModifierKeys.Alt, (s, e) => SaveImage_Click(null, null));
            AddKeyBinding(Key.T, ModifierKeys.Control, (s, e) => SaveThumbnails_Click(null, null));
            AddKeyBinding(Key.O, ModifierKeys.Control | ModifierKeys.Shift, (s, e) => File_Options_Click(null, null));
            AddKeyBinding(Key.Enter, ModifierKeys.Alt, (s, e) => Properties_Click(null, null));
            AddKeyBinding(Key.L, ModifierKeys.Control, (s, e) => OpenFileLocation_Click(null, null));
        }

        private void AddKeyBinding(Key key, ModifierKeys mod, ExecutedRoutedEventHandler handler)
        {
            var cmd = new RoutedCommand();
            InputBindings.Add(new KeyBinding(cmd, new KeyGesture(key, mod)));
            CommandBindings.Add(new CommandBinding(cmd, handler));
        }


        // ===== Cursor change on edges =====
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                return;

            Point pos = e.GetPosition(this);

            if (pos.X <= edge && pos.Y <= edge)
                this.Cursor = Cursors.SizeNWSE;
            else if (pos.X >= ActualWidth - edge && pos.Y <= edge)
                this.Cursor = Cursors.SizeNESW;
            else if (pos.X <= edge && pos.Y >= ActualHeight - edge)
                this.Cursor = Cursors.SizeNESW;
            else if (pos.X >= ActualWidth - edge && pos.Y >= ActualHeight - edge)
                this.Cursor = Cursors.SizeNWSE;
            else if (pos.X <= edge || pos.X >= ActualWidth - edge)
                this.Cursor = Cursors.SizeWE;
            else if (pos.Y <= edge || pos.Y >= ActualHeight - edge)
                this.Cursor = Cursors.SizeNS;
            else
                this.Cursor = Cursors.Arrow;
        }


        //-------------------------------------- Timer Tick Event ------------------------------------------------------
        //private void Timer_Tick(object? sender, EventArgs e)
        //{
        //    if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
        //    {
        //        progressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        //        progressSlider.Value = mediaPlayer.Position.TotalSeconds;

        //        SliderHandler.UpdateSliderVisual(progressSlider);

        //        TimeSpan current = mediaPlayer.Position;
        //        TimeSpan total = mediaPlayer.NaturalDuration.TimeSpan;
        //        TimeSpan remaining = total - current;
        //        lblDuration.Content = $"{current:hh\\:mm\\:ss} / {total:hh\\:mm\\:ss} (-{remaining:hh\\:mm\\:ss})";

        //        mediaController.LastPosition = mediaPlayer.Position;

        //        mediaController.UpdateSubtitle(mediaPlayer.Position);


        //        // ---------------- Update Statistics Panel ----------------
        //        if (statisticsPanel.Visibility == Visibility.Visible && mediaPlayer.Source != null)
        //        {
        //            string path = Uri.UnescapeDataString(mediaPlayer.Source.LocalPath);
        //            var stats = FFmpegStatisticHelper.GetVideoStatistics(path);

        //            txtResolution.Text = $"Resolution: {stats.Resolution}";
        //            txtFPS.Text = $"FPS: {stats.FPS}";
        //            txtBitrate.Text = $"Bitrate: {stats.Bitrate}";
        //            txtBuffer.Text = "Buffer: N/A";  // optional
        //            txtJitter.Text = "Jitter: N/A";  // optional
        //        }
        //    }

        //}


        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (mediaPlayer.Source == null || !mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressSlider.Value = 0;
                lblDuration.Content = "00:00:00 / 00:00:00";
                return;
            }
            if (!isDraggingSlider && !isClickSeeking)
            {
                         // ---------------- Slider ----------------
                progressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                progressSlider.Value = mediaPlayer.Position.TotalSeconds;

                SliderHandler.UpdateSliderVisual(progressSlider);

                // ---------------- Time Labels ----------------
                TimeSpan current = mediaPlayer.Position;
                TimeSpan total = mediaPlayer.NaturalDuration.TimeSpan;
                TimeSpan remaining = total - current;
                lblDuration.Content = $"{current:hh\\:mm\\:ss} / {total:hh\\:mm\\:ss} (-{remaining:hh\\:mm\\:ss})";

                // ---------------- Subtitle ----------------
                mediaController.UpdateSubtitle(mediaPlayer.Position);

                // ---------------- Statistics ----------------
                if (statisticsPanel.Visibility == Visibility.Visible)
                {
                    string path = Uri.UnescapeDataString(mediaPlayer.Source.LocalPath);
                    var stats = FFmpegStatisticHelper.GetVideoStatistics(path);

                    txtResolution.Text = $"Resolution: {stats.Resolution}";
                    txtFPS.Text = $"FPS: {stats.FPS}";
                    txtBitrate.Text = $"Bitrate: {stats.Bitrate}";
                    txtBuffer.Text = "Buffer: N/A";  // optional
                    txtJitter.Text = "Jitter: N/A";  // optional
                }
            }
        }



        //-------------------------------------- Progress Slider -------------------------------------------------------
        private void progressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SliderHandler.OnSliderMouseDown(ref isDraggingSlider);
            isClickSeeking = true;
        }

        private void progressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SliderHandler.OnSliderMouseUp(progressSlider, ref isDraggingSlider, mediaPlayer);
            isClickSeeking = false;
        }

        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isDraggingSlider) return;
            SliderHandler.UpdateSliderVisual(progressSlider);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            SliderHandler.OnThumbDrag(progressSlider, e);
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(progressSlider.Value);
        }

        private void SliderContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SliderHandler.OnSliderClick(progressSlider, mediaPlayer, e);
        }


        //-------------------------------------- UI Buttons -------------------------------------------------------------

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaController.PlayCurrent(); // fast playback
            UpdateVisualizer();
        }


        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaController.Pause();
            visualizer.StopAudioCapture();
        }


        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaController.Stop();
            visualizer.StopAudioCapture();
            visualizer.Stop();
            mediaController.SaveLastSession();
        }



        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            mediaController.Next(); // advances the index
            mediaController.PlayCurrent(); 
            UpdateVisualizer();
            mediaController.SaveLastSession();
        }




        private async void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            mediaController.Previous(); // moves to previous
            mediaController.PlayCurrent();
            UpdateVisualizer(); // for Audio Visualizer
            mediaController.SaveLastSession();
        }
        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            mediaController.ToggleShuffle();
            //MessageBox.Show(mediaController.IsShuffle ? "Shuffle ON" : "Shuffle OFF", "Shuffle Mode");
            ViewMenuHandler.ShowToast(this, mediaController.IsShuffle ? "Shuffle ON" : "Shuffle OFF");
        }






        public void PlayPlaylistTrack(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    MessageBox.Show("Selected track not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Use your existing MediaController logic
                mediaController.PlayFromPlaylist(filePath);

                // Update visualizer and UI
                UpdateVisualizer();

                // Update embedded subtitle menu after media loads
                // Update embedded subtitle menu after media loads
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateEmbeddedSubtitlesMenu();
                }), DispatcherPriority.ApplicationIdle);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to play this track.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //-------------------------------------- File Menu -------------------------------------------------------------
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            FileMenuHandler.BtnOpen_Click(this);
        }

        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            FileMenuHandler.OpenDirectory_Click(this);
        }

        private void OpenFileUrl_Click(object sender, RoutedEventArgs e)
        {
            FileMenuHandler.OpenFileUrl_Click(this);
        }

        private void SaveCopy_Click(object sender, RoutedEventArgs e)
        {
            FileMenuHandler.SaveCopy_Click(this);
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            FileMenuHandler.SaveImage_Click(this);
        }

        //-------------------------------------- Visualizer -----------------------------------------------------------
        private void Visualizer_SelectPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string tagStr && int.TryParse(tagStr, out int idx))
                selectedVisualizerPreset = idx;
            else if (sender is MenuItem mi2 && mi2.Tag is int idx2)
                selectedVisualizerPreset = idx2;

            visualizer.SetPreset(selectedVisualizerPreset);
        }

        public async void UpdateVisualizer()
        {
            // Check if the current file is audio
            if (!mediaController.IsAudioFile(mediaController.CurrentFile))
            {
                visualizer.StopAudioCapture();
                visualizer.Stop();
                return;
            }

            // Allow MediaElement to fully start audio output
            await Task.Delay(150);

            // Apply current preset
            visualizer.SetPreset(selectedVisualizerPreset);

            // Start visualizer rendering only once
            if (!visualizer.IsRunning)
                visualizer.Start();

            // Restart audio capture only ONCE
            visualizer.StartAudioCapture();
        }


        //-------------------------------------- Window Events --------------------------------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mediaController.SaveLastSession();


            // 🧹 CLEAN UP TEMP FILES
            Debug.WriteLine("🧹 Application closing - cleaning up temp files...");
            mediaController.CleanupTempFiles();

            // Save window size/position
            if (Settings.Default.RememberWindow)
            {
                Settings.Default.WindowWidth = this.Width;
                Settings.Default.WindowHeight = this.Height;
                Settings.Default.WindowLeft = this.Left;
                Settings.Default.WindowTop = this.Top;
                Settings.Default.Save();
            }

        }

        private async void LoadLastSession()
        {
            mediaController.LoadLastSession();
            if (!string.IsNullOrEmpty(mediaController.LastFilePath))
            {
               mediaController.PlayCurrent();
                UpdateVisualizer();
            }
        }



        //--------------------------------------- Event Handlers from Windows Event Class ----------------------------------------------
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowEvents.TitleBar_MouseDown(this, e);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowEvents.BtnMinimize_Click(this);
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowEvents.BtnMaximize_Click(this);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            WindowEvents.BtnClose_Click(this, mediaController);
        }


        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            WindowEvents.CloseButton_MouseEnter();
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            WindowEvents.CloseButton_MouseLeave();
        }
        //--------------------------------------- Menu Events from MenuEvents Class -----------------------------------------
        private void QuickOpenFile_Click(object sender, RoutedEventArgs e)
        {
            FileMenuHandler.QuickOpenFile_Click(sender, e, mediaController);
        }

        private void OpenDVD_Click(object sender, RoutedEventArgs e)
        => MenuEvents.OpenDVD_Click(sender, e, this.mediaController);

        private void OpenDevice_Click(object sender, RoutedEventArgs e)
            => MenuEvents.OpenDevice_Click(sender, e, this.mediaController);

        private void OpenDisc_Click(object sender, RoutedEventArgs e)
            => MenuEvents.OpenDisc_Click(sender, e, mediaController);

        private void RecentFiles_Click(object sender, RoutedEventArgs e)
    => MenuEvents.RecentFiles_Click(sender, e, this.mediaController);


        private void RecentFilesMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            MenuEvents.RecentFilesMenu_MouseEnter(sender, e, mediaController);
        }

        //private void SaveThumbnails_Click(object sender, RoutedEventArgs e)
        //{
        //    if (mediaPlayer.Source == null)
        //    {
        //        MessageBox.Show("No video currently playing!");
        //        return;
        //    }

        //    string currentVideo = mediaPlayer.Source.LocalPath;
        //    MenuEvents.SaveThumbnails(currentVideo);
        //}


        private async void SaveThumbnails_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null)
            {
                MessageBox.Show("No video loaded!");
                return;
            }

            string videoPath = mediaPlayer.Source.LocalPath;
            double durationSeconds = Modules.ThumbnailHelper.GetVideoDuration(
                videoPath,
                @"C:\ffmpeg\bin\ffprobe.exe"
            );
            TimeSpan videoLength = TimeSpan.FromSeconds(durationSeconds);

            ThumbnailSettingsWindow settingsWindow = new ThumbnailSettingsWindow(videoLength)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() != true)
                return;

            int scale = settingsWindow.SelectedResolution;
            int rows = settingsWindow.SelectedRows;
            int cols = settingsWindow.SelectedColumns;

            if (sender is MenuItem menuItem)
                menuItem.IsEnabled = false;

            ThumbnailProgressBar.Visibility = Visibility.Visible;
            ThumbnailProgressBar.Value = 0;

            var progress = new Progress<double>(v => ThumbnailProgressBar.Value = v);

            try
            {
                bool success = await ThumbnailHelper.SaveThumbnailsAsync(
                    videoPath: videoPath,
                    progress: progress,
                    rows: rows,
                    cols: cols,
                    scale: scale
                );

                // ⚠️ Only show success message when TRUE
                if (success)
                {
                    MessageBox.Show(
                        "Thumbnails saved successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                // ❌ Do nothing on failure, ThumbnailHelper already showed error
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unexpected error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                ThumbnailProgressBar.Visibility = Visibility.Collapsed;

                if (sender is MenuItem mi)
                    mi.IsEnabled = true;
            }
        }

        //--------------------------------------- Check  for Properties of the current file --------------------------------


        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null)
            {
                MessageBox.Show("No file currently playing!");
                return;
            }

            // Get full file path from MediaElement
            string currentFile = Uri.UnescapeDataString(mediaPlayer.Source.LocalPath);

            // Call MenuEvents method
            MenuEvents.Properties_Click(currentFile, this);
        }


        //----------------------------------- Open File Location in Explorer ----------------------------------------

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null)
            {
                MessageBox.Show("No file currently playing!");
                return;
            }

            string currentFile = Uri.UnescapeDataString(mediaPlayer.Source.LocalPath);
            MenuEvents.OpenFileLocation_Click(currentFile);
        }

        // --------------------------------------- Exit Application -----------------------------------------------
        private void Exit_Click(object sender, RoutedEventArgs e)
            => MenuEvents.Exit_Click(sender, e);

        private void MenuSelectAudioTrack_Click(object sender, RoutedEventArgs e)
            => MenuEvents.MenuSelectAudioTrack_Click(sender, e);

        private void MenuSelectSubtitleTrack_Click(object sender, RoutedEventArgs e)
            => MenuEvents.MenuSelectSubtitleTrack_Click(sender, e);

        private void MenuSelectVideoQuality_Click(object sender, RoutedEventArgs e)
            => MenuEvents.MenuSelectVideoQuality_Click(sender, e);

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
            => MenuEvents.MenuAbout_Click(sender, e);

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            MediaPlayerEvents.MediaOpened(sender, e);

            // Auto-size window to video resolution (with delay to ensure video is loaded)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Small delay to ensure NaturalVideoWidth/Height are available
                Task.Delay(100).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => AutoSizeWindowToVideo());
                });
            }), DispatcherPriority.Background);

            // Update embedded subtitle menu when media loads
            UpdateEmbeddedSubtitlesMenu();
        }



        //----------------------------- Color Change Menu ----------------------------------------------
        private void Menu_ColorChange(object sender, RoutedEventArgs e)
            => MenuEvents.MenuColorChange(sender, e);

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
            => MediaPlayerEvents.MediaEnded(sender, e);

        private void BtnBackward_Click(object sender, RoutedEventArgs e)
    => NavigationEvents.BtnBackward_Click(sender, e);

        private void BtnForward_Click(object sender, RoutedEventArgs e)
            => NavigationEvents.BtnForward_Click(sender, e);
        //----------------------------------- This Part is For View Menu ------------------------------------------------------

        private void View_HideMenu_Click(object sender, RoutedEventArgs e)
    => ViewMenuHandler.HideMenu_Click(sender, e, this);

        private void View_SeekBar_Click(object sender, RoutedEventArgs e)
    => ViewMenuHandler.SeekBar_Click(sender, e, this);

        private void View_Controls_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Controls_Click(sender, e, this);

        private void View_Information_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Information_Click(sender, e, this);

        private void View_Statistics_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Statistics_Click(sender, e, this);

        private void View_Status_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Status_Click(sender, e, this);

        private void View_LoadSubtitle_Click(object sender, RoutedEventArgs e)
    => ViewMenuHandler.LoadSubtitle_Click(sender, e, this);

        private void View_DownloadSubtitle_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.DownloadSubtitle_Click(sender, e, this);


        private void View_Subresync_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Subresync_Click(sender, e, this);

        private void View_Playlist_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Playlist_Click(sender, e, this);

        private void View_Capture_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.Capture_Click(sender, e, this);



        private void View_FullScreen_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.FullScreen_Click(sender, e, this);

        private void View_OnTop_Click(object sender, RoutedEventArgs e)
            => ViewMenuHandler.OnTop_Click(sender, e, this);



        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply saved theme safely here
            var savedColor = ThemeManager.LoadColor();
            ThemeManager.ApplyTheme(this, savedColor);
        }


        // Video Scaling

        // ================== Stretch ==================
        private void Stretch_Original_Click(object sender, RoutedEventArgs e) => _videoScale.SetOriginal();
        private void Stretch_Fit_Click(object sender, RoutedEventArgs e) => _videoScale.SetFit();
        private void Stretch_Fill_Click(object sender, RoutedEventArgs e) => _videoScale.SetFill();
        private void Stretch_Zoom_Click(object sender, RoutedEventArgs e) => _videoScale.SetZoom();

        // ================== Aspect Ratio ==================
        private void Aspect_16_9_Click(object sender, RoutedEventArgs e) => _videoScale.SetAspectRatio16_9();
        private void Aspect_4_3_Click(object sender, RoutedEventArgs e) => _videoScale.SetAspectRatio4_3();
        private void Aspect_16_12_Click(object sender, RoutedEventArgs e) => _videoScale.SetAspectRatio16_12();
        private void Aspect_9_6_Click(object sender, RoutedEventArgs e) => _videoScale.SetAspectRatio9_6();
        private void Aspect_Original_Click(object sender, RoutedEventArgs e) => _videoScale.SetAspectRatioOriginal();






        // Called when user clicks on the playback border for displaying the Context Menu.
        private void PlaybackBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PlaybackBorder.ContextMenu != null)
            {
                PlaybackBorder.ContextMenu.PlacementTarget = PlaybackBorder;
                PlaybackBorder.ContextMenu.IsOpen = true;
                e.Handled = true; // prevents further bubbling
            }
        }





        private void File_Options_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow.SettingsWindow();
                settingsWindow.Owner = this; // Makes it modal relative to MainWindow
                settingsWindow.ShowDialog(); // Opens as modal dialog
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open Settings window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        //public void ApplySubtitleSettings()
        //{
        //    // Size
        //    SubtitleTextBlock.FontSize = Settings.Default.SubtitleFontSize;

        //    // Color
        //    var converter = new BrushConverter();
        //    SubtitleTextBlock.Foreground = (Brush)converter.ConvertFromString(Settings.Default.SubtitleFontColor);

        //    // Background opacity
        //    double opacity = Settings.Default.SubtitleBackgroundOpacity / 100.0;
        //    SubtitleTextBlock.Background = new SolidColorBrush(Color.FromArgb(
        //        (byte)(opacity * 255), 0, 0, 0)); // Black background

        //    // Position (0 = top, 100 = bottom)
        //    double offset = (100 - Settings.Default.SubtitlePosition) * 4.5;
        //    SubtitleTextBlock.Margin = new Thickness(10, 0, 10, offset);

        //    // Enable/Disable
        //    SubtitleTextBlock.Visibility = Settings.Default.EnableSubtitles
        //        ? Visibility.Visible
        //        : Visibility.Collapsed;
        //}

        public void ApplySubtitleSettings()
        {
            // Font size
            SubtitleTextBlock.FontSize = Settings.Default.SubtitleFontSize;

            // Color
            var color = (Color)ColorConverter.ConvertFromString(Settings.Default.SubtitleFontColor);
            SubtitleTextBlock.Foreground = new SolidColorBrush(color);

            // Vertical position (as Margin)
            double percentage = Settings.Default.SubtitlePosition;
            SubtitleTextBlock.Margin = new Thickness(10, 0, 10, percentage);

            // Background opacity
            SubtitleTextBlock.Background = new SolidColorBrush(Color.FromArgb(
                (byte)(Settings.Default.SubtitleBackgroundOpacity * 2.55), 0, 0, 0));
        }


        private void VideoContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mediaPlayer == null || SubtitleTextBlock == null)
                return;

            // Video natural size (after media opened)
            double videoW = mediaPlayer.NaturalVideoWidth;
            double videoH = mediaPlayer.NaturalVideoHeight;

            if (videoW == 0 || videoH == 0)
                return;

            // Container size
            double containerW = VideoContainer.ActualWidth;
            double containerH = VideoContainer.ActualHeight;

            // Calculate rendered video size (Uniform Stretch)
            double scale = Math.Min(containerW / videoW, containerH / videoH);
            double renderedW = videoW * scale;
            double renderedH = videoH * scale;

            // Letterbox vertical offset
            double letterBox = (containerH - renderedH) / 2;

            // Keep subtitles just above the video bottom (5% inside video area)
            double offset = letterBox + (renderedH * 0.05);

            SubtitleTextBlock.Margin = new Thickness(0, 0, 0, offset);

            // Scale font
            double baseHeight = 420;
            double scaleFactor = renderedH / baseHeight;
            SubtitleTextBlock.FontSize = Math.Max(12, 24 * scaleFactor);
        }
        //------------------------------------------------------ -------------------------------------------


        private void ApplyInterfaceSettings()
        {
            // Start Maximized
            if (Settings.Default.StartMaximized)
                this.WindowState = WindowState.Maximized;

            // Always on Top
            this.Topmost = Settings.Default.AlwaysOnTop;

            // Auto-size to video (you'll need to add this setting)
            // if (Settings.Default.AutoResizeToVideo) 
            // {
            //     // Enable auto-sizing
            // }
            // Remember Window Size & Position
            if (Settings.Default.RememberWindow)
            {
                if (Settings.Default.WindowWidth > 0)
                    this.Width = Settings.Default.WindowWidth;

                if (Settings.Default.WindowHeight > 0)
                    this.Height = Settings.Default.WindowHeight;

                if (Settings.Default.WindowLeft >= 0)
                    this.Left = Settings.Default.WindowLeft;

                if (Settings.Default.WindowTop >= 0)
                    this.Top = Settings.Default.WindowTop;
            }
        }

        private void ApplyAccentColor(string accent)
        {
            string colorHex = accent switch
            {
                "blue" => "#2196F3",
                "red" => "#F44336",
                "green" => "#4CAF50",
                "orange" => "#FF9800",
                _ => "#2196F3"
            };

            // Update the brush dynamically
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));

            // Optional: lighten/darken for other accent variants
            Application.Current.Resources["AccentLightBrush"] = new SolidColorBrush(((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color) { Opacity = 0.5 };
            Application.Current.Resources["AccentDarkBrush"] = new SolidColorBrush(((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color) { Opacity = 0.8 };
        }



        public void ResetUIPlaybackState()
        {
            timer.Stop();

            progressSlider.Value = 0;
            progressSlider.Maximum = 1;

            lblDuration.Content = "00:00:00 / 00:00:00";
        }

        public void RestartUITimer()
        {
            timer.Stop();
            timer.Start();
        }

        public void StopVisualizer()
        {
            visualizer?.StopAudioCapture();
            visualizer?.Stop();
        }



        // In MainWindow.xaml.cs
        public void SetCustomTitle(string title)
        {
            txtTitle.Text = title; // txtTitle is your TextBlock in the custom title bar
        }


        // Add these methods to your MainWindow class

        public void UpdateEmbeddedSubtitlesMenu()
        {
            try
            {
                // Find the subtitles menu
                var subtitleMenu = FindName("SubtitleMenu") as MenuItem;
                if (subtitleMenu == null) return;

                // Clear previous embedded subtitle items
                // Clear previous embedded subtitle items
var itemsToRemove = new List<MenuItem>();

foreach (var item in subtitleMenu.Items.OfType<MenuItem>())
{
    string tag = item.Tag?.ToString() ?? "";
    if (tag.StartsWith("embedded_") || tag.StartsWith("language_") || tag == "other_languages")
    {
        itemsToRemove.Add(item);
    }
}

foreach (var item in itemsToRemove)
{
    subtitleMenu.Items.Remove(item);
}

                // Add embedded subtitle tracks if any
                if (mediaController.EmbeddedSubtitleTracks.Any())
                {
                    // Add separator
                    subtitleMenu.Items.Add(new Separator());

                    // Add embedded subtitle header
                    var embeddedHeader = new MenuItem
                    {
                        Header = "📝 Embedded Subtitles",
                        IsEnabled = false,
                        Tag = "embedded_header",
                        FontWeight = FontWeights.Bold
                    };
                    subtitleMenu.Items.Add(embeddedHeader);

                    // Group tracks by language for better organization
                    var languageGroups = mediaController.EmbeddedSubtitleTracks
                        .GroupBy(t => t.Language)
                        .OrderBy(g => g.Key)
                        .ToList();

                    // Define popular languages (show these first)
                    var popularLanguages = new[] {
                "English", "Spanish", "French", "German", "Italian",
                "Portuguese", "Russian", "Chinese", "Japanese", "Korean",
                "Arabic", "Hindi", "Turkish", "Polish", "Dutch"
            };

                    var popularTracks = languageGroups.Where(g => popularLanguages.Contains(g.Key));
                    var otherTracks = languageGroups.Where(g => !popularLanguages.Contains(g.Key));

                    // Add popular languages directly
                    foreach (var languageGroup in popularTracks.OrderBy(g => Array.IndexOf(popularLanguages, g.Key)))
                    {
                        if (languageGroup.Count() == 1)
                        {
                            // Single track for this language - add directly
                            var track = languageGroup.First();
                            AddTrackToMenu(subtitleMenu, track);
                        }
                        else
                        {
                            // Multiple tracks - create language submenu
                            var languageMenu = new MenuItem
                            {
                                Header = $"{GetLanguageEmoji(languageGroup.Key)} {languageGroup.Key} ({languageGroup.Count()})",
                                Tag = $"language_{languageGroup.Key}",
                                ToolTip = $"{languageGroup.Count()} {languageGroup.Key} subtitle tracks"
                            };

                            // Sort tracks: default first, then by name
                            var sortedTracks = languageGroup
                                .OrderByDescending(t => t.IsDefault)
                                .ThenBy(t => t.Name)
                                .ToList();

                            foreach (var track in sortedTracks)
                            {
                                AddTrackToSubmenu(languageMenu, track);
                            }

                            subtitleMenu.Items.Add(languageMenu);
                        }
                    }

                    // Add "Other Languages" submenu if there are other languages
                    if (otherTracks.Any())
                    {
                        var otherMenu = new MenuItem
                        {
                            Header = "🌍 Other Languages",
                            Tag = "other_languages",
                            ToolTip = $"{otherTracks.Sum(g => g.Count())} tracks in {otherTracks.Count()} languages"
                        };

                        foreach (var languageGroup in otherTracks.OrderBy(g => g.Key))
                        {
                            if (languageGroup.Count() == 1)
                            {
                                // Single track - add directly to "Other Languages"
                                var track = languageGroup.First();
                                AddTrackToSubmenu(otherMenu, track);
                            }
                            else
                            {
                                // Multiple tracks - create submenu for this language
                                var subLanguageMenu = new MenuItem
                                {
                                    Header = $"{GetLanguageEmoji(languageGroup.Key)} {languageGroup.Key} ({languageGroup.Count()})",
                                    ToolTip = $"{languageGroup.Count()} {languageGroup.Key} subtitle tracks"
                                };

                                var sortedTracks = languageGroup
                                    .OrderByDescending(t => t.IsDefault)
                                    .ThenBy(t => t.Name)
                                    .ToList();

                                foreach (var track in sortedTracks)
                                {
                                    AddTrackToSubmenu(subLanguageMenu, track);
                                }

                                otherMenu.Items.Add(subLanguageMenu);
                            }
                        }

                        subtitleMenu.Items.Add(otherMenu);
                    }

                    // Add "No Subtitles" option at the end
                    subtitleMenu.Items.Add(new Separator());
                    var noSubsMenuItem = new MenuItem
                    {
                        Header = "🚫 No Subtitles",
                        Tag = "no_subtitles"
                    };
                    noSubsMenuItem.Click += DisableSubtitles_Click;
                    subtitleMenu.Items.Add(noSubsMenuItem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating embedded subtitles menu: {ex.Message}");
            }
        }

        private void AddTrackToMenu(MenuItem parentMenu, EmbeddedSubtitleTrack track)
        {
            string displayName = CreateDisplayName(track);

            var menuItem = new MenuItem
            {
                Header = displayName,
                Tag = $"embedded_{track.TrackIndex}",
                ToolTip = CreateTooltip(track),
                FontWeight = track.IsDefault ? FontWeights.Bold : FontWeights.Normal
            };
            menuItem.Click += EmbeddedSubtitleMenuItem_Click;
            parentMenu.Items.Add(menuItem);
        }

        private void AddTrackToSubmenu(MenuItem parentMenu, EmbeddedSubtitleTrack track)
        {
            string displayName = CreateDisplayName(track);

            var menuItem = new MenuItem
            {
                Header = displayName,
                Tag = $"embedded_{track.TrackIndex}",
                ToolTip = CreateTooltip(track),
                FontWeight = track.IsDefault ? FontWeights.Bold : FontWeights.Normal
            };
            menuItem.Click += EmbeddedSubtitleMenuItem_Click;
            parentMenu.Items.Add(menuItem);
        }

        private string CreateDisplayName(EmbeddedSubtitleTrack track)
        {
            string displayName;

            // Clean up the track name
            string cleanName = track.Name
                .Replace(track.Language, "") // Remove language if redundant
                .Replace("Subtitle", "Track") // More user-friendly
                .Replace("subrip", "") // Remove codec info
                .Replace("()", "") // Clean empty parentheses
                .Replace("[]", "") // Clean empty brackets
                .Trim();

            // Remove leading/trailing punctuation
            if (cleanName.StartsWith("-") || cleanName.StartsWith(":") || cleanName.StartsWith("."))
                cleanName = cleanName.Substring(1).Trim();
            if (cleanName.EndsWith("-") || cleanName.EndsWith(":") || cleanName.EndsWith("."))
                cleanName = cleanName.Substring(0, cleanName.Length - 1).Trim();

            // Create display name
            if (!string.IsNullOrEmpty(cleanName) && cleanName != track.TrackIndex.ToString())
            {
                displayName = $"{GetTrackEmoji(track)} {cleanName}";
            }
            else
            {
                displayName = $"{GetTrackEmoji(track)} Track {track.TrackIndex + 1}";
            }

            // Add default indicator
            if (track.IsDefault)
                displayName += " ★";

            return displayName;
        }

        private string CreateTooltip(EmbeddedSubtitleTrack track)
        {
            var tooltip = new System.Text.StringBuilder();
            tooltip.Append($"Language: {track.Language}");
            tooltip.Append($"{Environment.NewLine}Codec: {track.Codec}");

            if (!string.IsNullOrEmpty(track.Name) && track.Name != $"Subtitle {track.TrackIndex}")
                tooltip.Append($"{Environment.NewLine}Description: {track.Name}");

            if (track.IsDefault)
                tooltip.Append($"{Environment.NewLine}Default track");

            return tooltip.ToString();
        }

        private string GetLanguageEmoji(string language)
        {
            return language.ToLower() switch
            {
                "english" => "🇺🇸",
                "spanish" => "🇪🇸",
                "french" => "🇫🇷",
                "german" => "🇩🇪",
                "italian" => "🇮🇹",
                "portuguese" => "🇵🇹",
                "russian" => "🇷🇺",
                "chinese" => "🇨🇳",
                "japanese" => "🇯🇵",
                "korean" => "🇰🇷",
                "arabic" => "🇸🇦",
                "hindi" => "🇮🇳",
                "turkish" => "🇹🇷",
                "polish" => "🇵🇱",
                "dutch" => "🇳🇱",
                _ => "🌐"
            };
        }

        private string GetTrackEmoji(EmbeddedSubtitleTrack track)
        {
            if (track.Name.Contains("SDH", StringComparison.OrdinalIgnoreCase) ||
                track.Name.Contains("hearing", StringComparison.OrdinalIgnoreCase))
                return "👂";

            if (track.Name.Contains("Forced", StringComparison.OrdinalIgnoreCase))
                return "🔤";

            if (track.Name.Contains("Commentary", StringComparison.OrdinalIgnoreCase))
                return "💬";

            return "📄";
        }

        private void EmbeddedSubtitleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Tag is string tag && tag.StartsWith("embedded_"))
            {
                if (int.TryParse(tag.Replace("embedded_", ""), out int trackIndex))
                {
                    // This uses your EXISTING subtitle system!
                    mediaController.LoadEmbeddedSubtitleAsExternal(trackIndex);
                }
            }
        }

        private void DisableSubtitles_Click(object sender, RoutedEventArgs e)
        {
            // This uses your EXISTING subtitle clearing system!
            mediaController.ClearSubtitles();
            ViewMenuHandler.ShowToast(this, "Subtitles disabled");
        }



        //======================================== Auto Size Window to Video Size ==========================================

        private (int width, int height) GetVideoResolution()
        {
            try
            {
                if (mediaPlayer.NaturalVideoWidth > 0 && mediaPlayer.NaturalVideoHeight > 0)
                {
                    return (mediaPlayer.NaturalVideoWidth, mediaPlayer.NaturalVideoHeight);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting video resolution: {ex.Message}");
            }
            return (0, 0);
        }



        private void AutoSizeWindowToVideo()
        {
            try
            {
                // Don't resize if in fullscreen, maximized, or if settings prevent it
                if (IsFullScreen || this.WindowState == WindowState.Maximized)
                    return;

                var (videoWidth, videoHeight) = GetVideoResolution();

                if (videoWidth == 0 || videoHeight == 0)
                    return;

                // Get screen information
                var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
                var screenWidth = screen.WorkingArea.Width;
                var screenHeight = screen.WorkingArea.Height;

                // UI element sizes (adjust these based on your actual UI)
                int titleBarHeight = 40;   // Your custom title bar
                int controlPanelHeight = 80; // Bottom controls
                int sidePadding = 20;      // Side margins

                int targetWindowWidth, targetWindowHeight;

                // Smart sizing based on resolution categories
                if (videoWidth >= 3840) // 4K - Scale down
                {
                    double scale = 0.6; // Show 4K at 60% size
                    targetWindowWidth = (int)(videoWidth * scale) + (sidePadding * 2);
                    targetWindowHeight = (int)(videoHeight * scale) + titleBarHeight + controlPanelHeight;
                }
                else if (videoWidth >= 2560) // 1440p - Scale slightly
                {
                    double scale = 0.8; // Show 1440p at 80% size
                    targetWindowWidth = (int)(videoWidth * scale) + (sidePadding * 2);
                    targetWindowHeight = (int)(videoHeight * scale) + titleBarHeight + controlPanelHeight;
                }
                else if (videoWidth >= 1920) // 1080p - Show at native size
                {
                    targetWindowWidth = videoWidth + (sidePadding * 2);
                    targetWindowHeight = videoHeight + titleBarHeight + controlPanelHeight;
                }
                else if (videoWidth >= 1280) // 720p - Show at native size
                {
                    targetWindowWidth = videoWidth + (sidePadding * 2);
                    targetWindowHeight = videoHeight + titleBarHeight + controlPanelHeight;
                }
                else // Lower resolutions (360p, 480p, etc.) - Don't make too small
                {
                    targetWindowWidth = Math.Max(videoWidth + (sidePadding * 2), 600);
                    targetWindowHeight = Math.Max(videoHeight + titleBarHeight + controlPanelHeight, 450);
                }

                // Respect absolute minimums from XAML
                targetWindowWidth = Math.Max(targetWindowWidth, 400);
                targetWindowHeight = Math.Max(targetWindowHeight, 300);

                // Ensure window fits on screen
                int maxWidth = (int)(screenWidth * 0.95);
                int maxHeight = (int)(screenHeight * 0.95);

                if (targetWindowWidth > maxWidth || targetWindowHeight > maxHeight)
                {
                    double widthRatio = (double)maxWidth / targetWindowWidth;
                    double heightRatio = (double)maxHeight / targetWindowHeight;
                    double scale = Math.Min(widthRatio, heightRatio);

                    targetWindowWidth = (int)(targetWindowWidth * scale);
                    targetWindowHeight = (int)(targetWindowHeight * scale);

                    // Re-apply minimums after scaling
                    targetWindowWidth = Math.Max(targetWindowWidth, 400);
                    targetWindowHeight = Math.Max(targetWindowHeight, 300);
                }

                // Apply with dispatcher to avoid threading issues
                this.Dispatcher.BeginInvoke(() =>
                {
                    this.Width = targetWindowWidth;
                    this.Height = targetWindowHeight;

                    // Keep window centered
                    this.Left = Math.Max(0, (screenWidth - targetWindowWidth) / 2);
                    this.Top = Math.Max(0, (screenHeight - targetWindowHeight) / 2);
                });

                Debug.WriteLine($"Video: {videoWidth}x{videoHeight} → Window: {targetWindowWidth}x{targetWindowHeight}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AutoSizeWindowToVideo: {ex.Message}");
            }
        }
    }


}

