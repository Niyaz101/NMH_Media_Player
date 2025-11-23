using NMH_Media_Player.Playback;
using NMH_Media_Player.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace NMH_Media_Player.SettingsTabs
{
    public partial class AdvancedTab : UserControl
    {
        public AdvancedTab()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            sliderBufferSize.Value = PlaybackSettingsManager.Instance.BufferSizeMb;
            chkEnableHardwareAcceleration.IsChecked = PlaybackSettingsManager.Instance.EnableHardwareAcceleration;
            chkEnableMultiThreadedDecoding.IsChecked = PlaybackSettingsManager.Instance.EnableMultiThreadedDecoding;
            chkUseCustomRenderPipeline.IsChecked = PlaybackSettingsManager.Instance.UseCustomRenderPipeline;
            chkEnableBetaFeatures.IsChecked = PlaybackSettingsManager.Instance.EnableBetaFeatures;
        }
            

        private void HardwareAcceleration_Changed(object sender, RoutedEventArgs e)
        {
            PlaybackSettingsManager.Instance.EnableHardwareAcceleration = chkEnableHardwareAcceleration.IsChecked ?? false;
            PlaybackSettingsManager.Instance.Save();
        }

        private void MultiThreaded_Changed(object sender, RoutedEventArgs e)
        {
            PlaybackSettingsManager.Instance.EnableMultiThreadedDecoding = chkEnableMultiThreadedDecoding.IsChecked ?? false;
            PlaybackSettingsManager.Instance.Save();
        }

        private void CustomRender_Changed(object sender, RoutedEventArgs e)
        {
            PlaybackSettingsManager.Instance.UseCustomRenderPipeline = chkUseCustomRenderPipeline.IsChecked ?? false;
            PlaybackSettingsManager.Instance.Save();
        }

        private void BetaFeatures_Changed(object sender, RoutedEventArgs e)
        {
            PlaybackSettingsManager.Instance.EnableBetaFeatures = chkEnableBetaFeatures.IsChecked ?? false;
            PlaybackSettingsManager.Instance.Save();
        }

       


        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Performance & Caching
            Settings.Default.BufferSize = (int)sliderBufferSize.Value;
            Settings.Default.EnableHardwareAcceleration = chkEnableHardwareAcceleration.IsChecked ?? false;
            Settings.Default.EnableMultiThreadedDecoding = chkEnableMultiThreadedDecoding.IsChecked ?? false;

            // Logging / Debug
            Settings.Default.EnableVerboseLogging = chkEnableVerboseLogging.IsChecked ?? false;
            Settings.Default.ShowDebugOverlay = chkShowDebugOverlay.IsChecked ?? false;

            // Experimental Features
            Settings.Default.EnableBetaFeatures = chkEnableBetaFeatures.IsChecked ?? false;
            Settings.Default.UseCustomRenderPipeline = chkUseCustomRenderPipeline.IsChecked ?? false;

            Settings.Default.Save();
            MessageBox.Show("Advanced settings saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }




        private void ClearLogFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (Directory.Exists(logFolder))
                {
                    Directory.Delete(logFolder, true);
                    MessageBox.Show("Log files cleared successfully!", "Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No log files found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error clearing log files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Optional: Mouse wheel scrolling if needed
        private void UserControl_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (MainScrollViewer != null)
            {
                double offset = MainScrollViewer.VerticalOffset - e.Delta;
                if (offset < 0) offset = 0;
                if (offset > MainScrollViewer.ScrollableHeight) offset = MainScrollViewer.ScrollableHeight;
                MainScrollViewer.ScrollToVerticalOffset(offset);
                e.Handled = true;
            }
        }
    }
}


