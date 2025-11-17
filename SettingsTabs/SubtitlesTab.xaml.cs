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
    public partial class SubtitlesTab : UserControl
    {
        public SubtitlesTab()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            chkEnableSubtitles.IsChecked = Settings.Default.EnableSubtitles;
            chkShowSubtitlesOnStartup.IsChecked = Settings.Default.ShowSubtitlesOnStartup;
            sliderSubtitleFontSize.Value = Settings.Default.SubtitleFontSize;
            sliderSubtitleBackground.Value = Settings.Default.SubtitleBackgroundOpacity;
            sliderSubtitlePosition.Value = Settings.Default.SubtitlePosition;

            // Load font color selection
            string color = Settings.Default.SubtitleFontColor;
            foreach (ComboBoxItem item in cmbSubtitleColor.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == color)
                {
                    cmbSubtitleColor.SelectedItem = item;
                    break;
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableSubtitles = chkEnableSubtitles.IsChecked ?? false;
            Settings.Default.ShowSubtitlesOnStartup = chkShowSubtitlesOnStartup.IsChecked ?? false;
            Settings.Default.SubtitleFontSize = (int)sliderSubtitleFontSize.Value;
            Settings.Default.SubtitleBackgroundOpacity = (int)sliderSubtitleBackground.Value;
            Settings.Default.SubtitlePosition = (int)sliderSubtitlePosition.Value;

            if (cmbSubtitleColor.SelectedItem is ComboBoxItem selectedColor)
                Settings.Default.SubtitleFontColor = selectedColor.Tag.ToString();

            Settings.Default.Save();

            MessageBox.Show("Subtitle settings saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

