using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NMH_Media_Player.Properties; // For Settings

namespace NMH_Media_Player.SettingsTabs
{
    public partial class PlaybackTab : UserControl
    {
        public PlaybackTab()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            chkAutoPlayNext.IsChecked = Settings.Default.AutoPlayNext;
            chkResumeLastPosition.IsChecked = Settings.Default.ResumeLastPosition;

            sliderPlaybackSpeed.Value = Settings.Default.PlaybackSpeed;
            sliderAudioDelay.Value = Settings.Default.AudioDelay;
            sliderBufferSize.Value = Settings.Default.BufferSize;

            chkLoopSingle.IsChecked = Settings.Default.LoopSingle;
            chkLoopPlaylist.IsChecked = Settings.Default.LoopPlaylist;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoPlayNext = chkAutoPlayNext.IsChecked ?? false;
            Settings.Default.ResumeLastPosition = chkResumeLastPosition.IsChecked ?? false;

            Settings.Default.PlaybackSpeed = sliderPlaybackSpeed.Value;
            Settings.Default.AudioDelay = (int)sliderAudioDelay.Value;
            Settings.Default.BufferSize = (int)sliderBufferSize.Value;

            Settings.Default.LoopSingle = chkLoopSingle.IsChecked ?? false;
            Settings.Default.LoopPlaylist = chkLoopPlaylist.IsChecked ?? false;

            Settings.Default.Save();
            MessageBox.Show("Playback settings saved!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
