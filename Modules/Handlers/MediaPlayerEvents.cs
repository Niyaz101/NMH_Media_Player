using NMH_Media_Player;
using NMH_Media_Player.Modules.Handlers;
using NMH_Media_Player.Playback;
using System.Windows;
using System.Windows.Controls;

public static class MediaPlayerEvents
{
    public static void MediaOpened(object sender, RoutedEventArgs e)
    {
        if (sender is MediaElement mediaPlayer && mediaPlayer.Source != null)
        {
            string path = Uri.UnescapeDataString(mediaPlayer.Source.LocalPath);
            var stats = FFmpegStatisticHelper.GetVideoStatistics(path);

            if (Application.Current.MainWindow is MainWindow window)
            {
                window.txtResolution.Text = $"Resolution: {stats.Resolution}";
                window.txtFPS.Text = $"FPS: {stats.FPS}";
                window.txtBitrate.Text = $"Bitrate: {stats.Bitrate}";

                var controller = window.mediaController;

                // ---- Resume position if needed ----
                if (controller.LastPosition > TimeSpan.Zero)
                {
                    mediaPlayer.Position = controller.LastPosition;
                    controller.LastPosition = TimeSpan.Zero;
                }

                // ---- Restart UI timer for new file ----
                window.RestartUITimer();

                // ---- Start playback only if PlayCurrent didn't already ----
                if (mediaPlayer.Position == TimeSpan.Zero)
                    mediaPlayer.Play();

                // ---- Visualizer ----
                if (controller.IsAudioFile(controller.GetCurrentFile()))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        window.UpdateVisualizer();
                    });
                }
            }
        }
    }




    //public static void MediaEnded(object sender, RoutedEventArgs e)
    //{
    //    // Example: Reset progress or do something when media ends
    //    if (Application.Current.MainWindow is MainWindow window)
    //    {
    //        window.progressSlider.Value = 0;
    //        window.lblDuration.Content = "00:00:00 / 00:00:00";
    //    }
    //}


    public static void MediaEnded(object sender, RoutedEventArgs e)
    {
        if (!(Application.Current.MainWindow is MainWindow window)) return;

        var controller = window.mediaController;
        if (controller == null || controller.GetCurrentFile() == null) return;

        // Reset progress UI
        window.progressSlider.Value = 0;
        window.lblDuration.Content = "00:00:00 / 00:00:00";

        var settings = PlaybackSettingsManager.Instance;

        if (settings.LoopSingle)
        {
            // Loop the same video
            controller.Player.Position = TimeSpan.Zero;
            controller.Player.Play();
        }
        else if (settings.AutoPlayNext)
        {
            // Move to next video in playlist
            controller.Next();
        }
        else
        {
            // Otherwise stop the player
            controller.Player.Stop();
        }
    }

}

