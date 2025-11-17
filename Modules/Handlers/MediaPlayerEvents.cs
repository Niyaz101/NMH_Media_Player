using NMH_Media_Player;
using NMH_Media_Player.Modules.Handlers;
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

                // ---------------- Resume position ----------------
                if (controller.LastPosition > TimeSpan.Zero)
                {
                    mediaPlayer.Position = controller.LastPosition;
                    controller.LastPosition = TimeSpan.Zero; // reset after seeking
                }

                // ---------------- Start playback ----------------
                mediaPlayer.Play();
            }
        }
    }



    public static void MediaEnded(object sender, RoutedEventArgs e)
    {
        // Example: Reset progress or do something when media ends
        if (Application.Current.MainWindow is MainWindow window)
        {
            window.progressSlider.Value = 0;
            window.lblDuration.Content = "00:00:00 / 00:00:00";
        }
    }
}

