using NMH_Media_Player.Playback;
using System;
using System.Windows;
using System.Windows.Controls;

namespace NMH_Media_Player.SettingsTabs
{
    public partial class PlaybackTab : UserControl
    {
        private readonly PlaybackSettingsManager settings = PlaybackSettingsManager.Instance;

        public PlaybackTab()
        {
            InitializeComponent();
            LoadSettingsToUI();

            // Optionally update UI when settings change elsewhere
            settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Keep UI in sync if settings changed externally (invoke on UI thread)
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => LoadSettingsToUI());
                return;
            }

            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // Defensive: guard against uninitialized controls (designer time)
            if (chkAutoPlayNext == null) return;

            chkAutoPlayNext.IsChecked = settings.AutoPlayNext;
            chkResumeLastPosition.IsChecked = settings.ResumeLastPosition;

            sliderPlaybackSpeed.Value = settings.PlaybackSpeed;
            sliderAudioDelay.Value = settings.AudioDelayMs;
            sliderBufferSize.Value = settings.BufferSizeMb;

            chkLoopSingle.IsChecked = settings.LoopSingle;
            chkLoopPlaylist.IsChecked = settings.LoopPlaylist;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read values from UI and update manager
                settings.UpdateFromUI(
                    chkAutoPlayNext.IsChecked ?? false,
                    chkResumeLastPosition.IsChecked ?? false,
                    sliderPlaybackSpeed.Value,
                    (int)sliderAudioDelay.Value,
                    (int)sliderBufferSize.Value,
                    chkLoopSingle.IsChecked ?? false,
                    chkLoopPlaylist.IsChecked ?? false
                );

                settings.Save();

                MessageBox.Show("Playback settings saved!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
