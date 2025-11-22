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
using NMH_Media_Player.Properties; 

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

            // Load font color
            if (!string.IsNullOrEmpty(Settings.Default.SubtitleFontColor))
            {
                var color = (Color)ColorConverter.ConvertFromString(Settings.Default.SubtitleFontColor);
                cpSubtitleColor.SelectedColor = color;
            }
        }


        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableSubtitles = chkEnableSubtitles.IsChecked ?? false;
            Settings.Default.ShowSubtitlesOnStartup = chkShowSubtitlesOnStartup.IsChecked ?? false;
            Settings.Default.SubtitleFontSize = (int)sliderSubtitleFontSize.Value;
            Settings.Default.SubtitleBackgroundOpacity = (int)sliderSubtitleBackground.Value;
            Settings.Default.SubtitlePosition = (int)sliderSubtitlePosition.Value;

            var selectedColor = cpSubtitleColor.SelectedColor ?? Colors.White;

            // Save the selected color in settings
            Settings.Default.SubtitleFontColor = selectedColor.ToString(); // optional, store as string
            Settings.Default.Save();


            

            // Apply to window live
            if (Application.Current.MainWindow is MainWindow mw)
                mw.ApplySubtitleSettings();

            MessageBox.Show("Subtitle settings saved successfully!", "Saved",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}

