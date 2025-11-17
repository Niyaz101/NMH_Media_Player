using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NMH_Media_Player.Modules.Handlers
{
    public class VolumeController
    {
        private readonly MediaElement mediaElement;
        private readonly Slider volumeSlider;
        private readonly Label speakerIcon;

        private bool isUserMuted = false;      // Muted via speaker icon
        private double previousVolume = 0.5;   // Stores last volume before mute
        private const double VolumeStep = 0.03; // 5% per scroll

        public VolumeController(MediaElement mediaElement, Slider volumeSlider, Label speakerIcon)
        {
            this.mediaElement = mediaElement;
            this.volumeSlider = volumeSlider;
            this.speakerIcon = speakerIcon;

            // Initialize volume
            mediaElement.Volume = volumeSlider.Value;
            UpdateVolumeIcon(volumeSlider.Value);

            // Event handlers
            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            speakerIcon.MouseLeftButtonDown += SpeakerIcon_Click;

            // Mouse wheel on media element
            mediaElement.PreviewMouseWheel += MediaElement_MouseWheel;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isUserMuted)
            {
                // Slider at 0 → muted; else volume follows slider
                mediaElement.Volume = e.NewValue;
            }

            UpdateVolumeIcon(e.NewValue);
        }

        private void SpeakerIcon_Click(object sender, MouseButtonEventArgs e)
        {
            if (isUserMuted)
            {
                // Unmute: restore volume from slider
                isUserMuted = false;
                mediaElement.Volume = volumeSlider.Value;
            }
            else
            {
                // Mute: store current volume
                isUserMuted = true;
                previousVolume = mediaElement.Volume;
                mediaElement.Volume = 0;
            }

            UpdateVolumeIcon(mediaElement.Volume);
        }

        private void MediaElement_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ignore if user muted via speaker icon
            if (isUserMuted) return;

            double newVolume = mediaElement.Volume + (e.Delta > 0 ? VolumeStep : -VolumeStep);

            // Clamp volume 0..1
            newVolume = Math.Max(0, Math.Min(1, newVolume));

            // Update slider and media element
            volumeSlider.Value = newVolume;
            mediaElement.Volume = newVolume;

            UpdateVolumeIcon(newVolume);
        }

        private void UpdateVolumeIcon(double volume)
        {
            if (isUserMuted || volume == 0)
                speakerIcon.Content = "🔇";
            else if (volume < 0.3)
                speakerIcon.Content = "🔈";
            else if (volume < 0.7)
                speakerIcon.Content = "🔉";
            else
                speakerIcon.Content = "🔊";
        }
    }
}
