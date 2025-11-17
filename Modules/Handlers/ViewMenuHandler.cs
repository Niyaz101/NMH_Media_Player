
using Microsoft.Win32;
using NMH_Media_Player.Modules;
using NMH_Media_Player.Modules.Handlers;
using NMH_Media_Player.SubtitlesViews;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;


namespace NMH_Media_Player.Handlers
{
    public static class ViewMenuHandler
    {




     
        public static void HideMenu_Click(object? sender, RoutedEventArgs? e, MainWindow window)
        {
            try
            {
                if (window == null || window.MainMenu == null)
                    return;

                // Hide menu if it's currently visible
                if (window.MainMenu.Visibility == Visibility.Visible)
                {
                    window.MainMenu.Visibility = Visibility.Collapsed;

                    // Optional: change the View->Hide Menu header text
                    if (sender is MenuItem mi)
                        mi.Header = "_Hide Menu (Ctrl+0)";
                }
                else
                {
                    // Show menu only if called via Ctrl+0 (sender is null)
                    if (sender == null)
                    {
                        window.MainMenu.Visibility = Visibility.Visible;

                        // Reset View->Hide Menu text
                        foreach (var item in window.MainMenu.Items)
                        {
                            if (item is MenuItem menuItem)
                            {
                                string? headerText = menuItem.Header?.ToString();
                                if (!string.IsNullOrEmpty(headerText) && headerText.StartsWith("_Show Menu"))
                                {
                                    menuItem.Header = "_Hide Menu";
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "View Menu Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }























        public static void SeekBar_Click(object? sender, RoutedEventArgs? e, MainWindow window)
        {
            try
            {
                if (window == null || window.progressSlider == null)
                    return;

                // Toggle visibility
                window.progressSlider.Visibility =
                    window.progressSlider.Visibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "View Menu Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        public static void Controls_Click(object? sender, RoutedEventArgs? e, MainWindow window)
        {
            try
            {
                // Safety check: ensure the window and control bar exist
                if (window == null || window.ControlBarPanel == null)
                    return;

                // Toggle visibility
                window.ControlBarPanel.Visibility =
                    window.ControlBarPanel.Visibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "View Menu Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }









        public static void Information_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            if (window == null || window.mediaPlayer == null)
                return;

            if (window.mediaPlayer.Source == null)
            {
                MessageBox.Show(window, "No file currently playing!", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get full file path from MediaElement
            string currentFile = Uri.UnescapeDataString(window.mediaPlayer.Source.LocalPath);

            // Call MenuEvents method
            MenuEvents.Properties_Click(currentFile, window);
        }










        public static void Statistics_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            if (window.statisticsPanel.Visibility == Visibility.Visible)
                window.statisticsPanel.Visibility = Visibility.Collapsed;
            else
                window.statisticsPanel.Visibility = Visibility.Visible;
        }



        public static void Status_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            if (window?.mediaController != null)
            {
                var controller = window.mediaController;

                string currentFile = string.IsNullOrEmpty(controller.CurrentFile)
                    ? "No file playing"
                    : System.IO.Path.GetFileName(controller.CurrentFile);

                TimeSpan position = TimeSpan.Zero;
                TimeSpan duration = TimeSpan.Zero;

                if (controller.Player != null && controller.Player.NaturalDuration.HasTimeSpan)
                {
                    position = controller.Player.Position;
                    duration = controller.Player.NaturalDuration.TimeSpan;
                }

                string statusMessage =
                    $"🎵 {currentFile}\n" +
                    $"⏱ {position:hh\\:mm\\:ss} / {duration:hh\\:mm\\:ss}\n" +
                    $"🔀 Shuffle: {(controller.IsShuffle ? "On" : "Off")}";

                // Call toast notification
                ShowToast(window, statusMessage);
            }
            else
            {
                ShowToast(window, "⚠ No media controller found!");
            }
        }

        // 🌟 SHOW TOAST METHOD
        public static void ShowToast(MainWindow window, string message, int durationSeconds = 3)
        {
            if (window?.ToastContainer == null)
            {
                MessageBox.Show("Toast container not found in MainWindow!");
                return;
            }

            // Create toast text block
            Border toastBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 25, 25, 25)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0,
                RenderTransform = new TranslateTransform(0, -30), // start slightly above
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(14, 8, 14, 8),
                    MaxWidth = 400
                }
            };

            window.ToastContainer.Children.Add(toastBorder);

            // ✨ Slide and fade-in animation
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var slideIn = new DoubleAnimation(-30, 0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };

            toastBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            (toastBorder.RenderTransform as TranslateTransform)?.BeginAnimation(TranslateTransform.YProperty, slideIn);

            // 🕓 Wait for duration, then fade out and slide up
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSeconds) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();

                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(600))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var slideOut = new DoubleAnimation(0, -30, TimeSpan.FromMilliseconds(600))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                fadeOut.Completed += (s2, e2) => window.ToastContainer.Children.Remove(toastBorder);

                toastBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                (toastBorder.RenderTransform as TranslateTransform)?.BeginAnimation(TranslateTransform.YProperty, slideOut);
            };
            timer.Start();
        }



        public static void LoadSubtitle_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            if (window == null)
            {
                MessageBox.Show("Main window reference is null.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "Load Subtitle File",
                Filter = "Subtitle Files (*.srt;*.sub;*.vtt)|*.srt;*.sub;*.vtt|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    // Ensure mediaController exists
                    if (window.mediaController == null)
                    {
                        ViewMenuHandler.ShowToast(window, "Media controller not available.", 3);
                        return;
                    }

                    window.mediaController.LoadSubtitles(filePath);

                    // Note: ShowToast is static and requires the window argument first
                    ViewMenuHandler.ShowToast(window, $"Subtitle loaded: {Path.GetFileName(filePath)}", 4);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(window, $"Failed to load subtitle:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static void DownloadSubtitle_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            var searchWindow = new SubtitleSearchWindow
            {
                Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Topmost = true
            };

            searchWindow.ShowDialog();
        }









        public static void Subresync_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            var subresyncWindow = new SubresyncWindow
            {
                Owner = window
            };

            if (subresyncWindow.ShowDialog() == true)
            {
                int shiftMs = subresyncWindow.ShiftMilliseconds;
                window.mediaController.ShiftSubtitles(shiftMs);
                ShowToast(window, $"Subtitles shifted by {shiftMs} ms");
            }
        }





        public static void Playlist_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            try
            {
                // Create and show PlaylistWindow as modal (blocks main window)
                var playlistWindow = new NMH_Media_Player.Modules.Playlists.PlaylistWindow
                {
                    Owner = window
                };

                playlistWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, $"Failed to open playlist manager:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }











        public static void Capture_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            try
            {
                // Simply call the FileMenu's SaveImage_Click
                FileMenuHandler.SaveImage_Click(window);
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, $"Error capturing snapshot:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        public static void FullScreen_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            try
            {
                if (!window.IsFullScreen)
                {
                    // Save current size & position
                    window._restoreTop = window.Top;
                    window._restoreLeft = window.Left;
                    window._restoreWidth = window.Width;
                    window._restoreHeight = window.Height;

                    // Enter fullscreen
                    window.WindowStyle = WindowStyle.None;
                    window.Topmost = true;
                    window.Left = 0;
                    window.Top = 0;
                    window.Width = SystemParameters.PrimaryScreenWidth;
                    window.Height = SystemParameters.PrimaryScreenHeight;
                    window.IsFullScreen = true;
                }
                else
                {
                    // Restore previous size & position
                    window.Topmost = false;
                    window.WindowStyle = WindowStyle.None; // keep your custom chrome
                    window.Left = window._restoreLeft;
                    window.Top = window._restoreTop;
                    window.Width = window._restoreWidth;
                    window.Height = window._restoreHeight;
                    window.IsFullScreen = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, $"Error toggling full screen:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        public static void OnTop_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            try
            {
                // Toggle Topmost property
                window.Topmost = !window.Topmost;

                // Optional: show a small notification
                string status = window.Topmost ? "enabled" : "disabled";
                MessageBox.Show(window, $"Always on top is now {status}.", "View Menu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, $"Error toggling always on top:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        public static void Options_Click(object sender, RoutedEventArgs e, MainWindow window)
        {
            MessageBox.Show(window, "Options clicked!", "View Menu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
