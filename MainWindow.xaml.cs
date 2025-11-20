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
using NMH_Media_Player.SettingsWindow;
using NMH_Media_Player.Thumbnails;
using NMH_Media_Player.VideoScaling;
using System;
using System.Collections.Generic;
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

        public MediaController mediaController { get; private set; }
        private AudioVisualizer visualizer;
        private int selectedVisualizerPreset = 0;

        private VideoScale _videoScale;



        

      


        //-------------------------------------- Constructor -----------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();





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

            // Ctrl+0 shows/hides the menu, this is for View Menu
            var toggleMenuCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(toggleMenuCommand, new KeyGesture(Key.D0, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(toggleMenuCommand,
                (s, e) => ViewMenuHandler.HideMenu_Click(null, null, this)));


            // Ctrl+1 shows/hides the Seek Bar (View Menu)
            var toggleSeekBarCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(toggleSeekBarCommand, new KeyGesture(Key.D1, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(toggleSeekBarCommand,
                (s, e) => ViewMenuHandler.SeekBar_Click(null, null, this)));



            // Ctrl+2 shows/hides the control bar
            var toggleControlsCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(toggleControlsCommand, new KeyGesture(Key.D2, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(toggleControlsCommand,
                (s, e) => ViewMenuHandler.Controls_Click(null, null, this)));


            // Ctrl+3 shows the Information window
            var infoCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(infoCommand, new KeyGesture(Key.D3, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(infoCommand,
                (s, e) => ViewMenuHandler.Information_Click(null, null, this)));



            //Ctrl+4 Show the Satistics of Playing File
            var statisticCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(statisticCommand, new KeyGesture(Key.D4, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(statisticCommand,
                (s, e) => ViewMenuHandler.Statistics_Click(null, null, this)));






            // Ctrl+5 Shows the Status of current File
            var showToastCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(showToastCommand, new KeyGesture(Key.D5, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(showToastCommand, (s, e) => ViewMenuHandler.Status_Click(null, null, this)));


            // Define the command
            var saveSubtitleCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(saveSubtitleCommand, new KeyGesture(Key.S, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(saveSubtitleCommand, (s, e) => ViewMenuHandler.LoadSubtitle_Click(null, null, this)));


            // Define the command for Subtitle Sync
            var subtitleSyncCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(subtitleSyncCommand, new KeyGesture(Key.D6, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(subtitleSyncCommand,
                (s, e) => ViewMenuHandler.Subresync_Click(null, null, this)));

            // Define the command for Playlist
            var playlistCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(playlistCommand, new KeyGesture(Key.D7, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(playlistCommand,
                (s, e) => ViewMenuHandler.Playlist_Click(null, null, this)));

            // Define the command for Capture
            var captureCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(captureCommand, new KeyGesture(Key.D8, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(captureCommand,
                (s, e) => ViewMenuHandler.Capture_Click(null, null, this)));


            // Define the command for Fullscreen
            var fullscreenCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(fullscreenCommand, new KeyGesture(Key.F11)));
            CommandBindings.Add(new CommandBinding(fullscreenCommand,
                (s, e) => ViewMenuHandler.FullScreen_Click(null, null, this)));



            // Define the Options command
            var optionsCommand = new RoutedCommand();
            InputBindings.Add(new KeyBinding(optionsCommand, new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(optionsCommand, (s, e) => File_Options_Click(null, null)));



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
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                progressSlider.Value = mediaPlayer.Position.TotalSeconds;

                SliderHandler.UpdateSliderVisual(progressSlider);

                TimeSpan current = mediaPlayer.Position;
                TimeSpan total = mediaPlayer.NaturalDuration.TimeSpan;
                TimeSpan remaining = total - current;
                lblDuration.Content = $"{current:hh\\:mm\\:ss} / {total:hh\\:mm\\:ss} (-{remaining:hh\\:mm\\:ss})";

                mediaController.LastPosition = mediaPlayer.Position;

                // ---------------- Update Statistics Panel ----------------
                if (statisticsPanel.Visibility == Visibility.Visible && mediaPlayer.Source != null)
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
        }

        private void progressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SliderHandler.OnSliderMouseUp(progressSlider, ref isDraggingSlider, mediaPlayer);
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

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            await mediaController.PlayCurrentWithFilterRealtimeAsync();
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
            await mediaController.PlayCurrentWithFilterRealtimeAsync(); // plays with filter
            UpdateVisualizer();
            mediaController.SaveLastSession();
        }




        private async void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            mediaController.Previous(); // moves to previous
            await mediaController.PlayCurrentWithFilterRealtimeAsync(); // plays with filter
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
        }

        private async void LoadLastSession()
        {
            mediaController.LoadLastSession();
            if (!string.IsNullOrEmpty(mediaController.LastFilePath))
            {
                await mediaController.PlayCurrentWithFilterRealtimeAsync(); // use the filter extraction
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
    => MediaPlayerEvents.MediaOpened(sender, e);


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

    }


}

