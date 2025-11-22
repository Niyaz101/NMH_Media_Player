using NMH_Media_Player.Properties; // For Settings
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NMH_Media_Player.SettingsTabs
{
    public partial class InterfaceTab : UserControl
    {
        public InterfaceTab()
        {
            InitializeComponent();
            LoadSettings();

        }

        private void LoadSettings()
        {
            // Load theme & accent
            string theme = Settings.Default.InterfaceTheme ?? "dark";
            foreach (ComboBoxItem item in cmbTheme.Items)
            {
                if (item.Tag.ToString() == theme)
                {
                    cmbTheme.SelectedItem = item;
                    break;
                }
            }

            string accent = Settings.Default.AccentColor ?? "blue";
            foreach (ComboBoxItem item in cmbAccentColor.Items)
            {
                if (item.Tag.ToString() == accent)
                {
                    cmbAccentColor.SelectedItem = item;
                    break;
                }
            }

            // Load window behavior
            chkStartMaximized.IsChecked = Settings.Default.StartMaximized;
            chkRememberWindow.IsChecked = Settings.Default.RememberWindow;
            chkAlwaysOnTop.IsChecked = Settings.Default.AlwaysOnTop;

            // Load player controls
            chkShowPlayPause.IsChecked = Settings.Default.ShowPlayPauseButton;
            chkShowStop.IsChecked = Settings.Default.ShowStopButton;
            chkShowVolume.IsChecked = Settings.Default.ShowVolumeSlider;

            // Load fonts
            sliderUIFont.Value = Settings.Default.UIFontSize;
            sliderSubtitleFont.Value = Settings.Default.SubtitleFontSize;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Save theme & accent
            if (cmbTheme.SelectedItem is ComboBoxItem selectedTheme)
                Settings.Default.InterfaceTheme = selectedTheme.Tag.ToString();

            if (cmbAccentColor.SelectedItem is ComboBoxItem selectedAccent)
                Settings.Default.AccentColor = selectedAccent.Tag.ToString();

            // Save window behavior
            Settings.Default.StartMaximized = chkStartMaximized.IsChecked ?? false;
            Settings.Default.RememberWindow = chkRememberWindow.IsChecked ?? false;
            Settings.Default.AlwaysOnTop = chkAlwaysOnTop.IsChecked ?? false;

            // Save player controls
            Settings.Default.ShowPlayPauseButton = chkShowPlayPause.IsChecked ?? false;
            Settings.Default.ShowStopButton = chkShowStop.IsChecked ?? false;
            Settings.Default.ShowVolumeSlider = chkShowVolume.IsChecked ?? false;

            // Save fonts
            Settings.Default.UIFontSize = (int)sliderUIFont.Value;
            Settings.Default.SubtitleFontSize = (int)sliderSubtitleFont.Value;

            Settings.Default.Save();
            MessageBox.Show("Interface settings saved!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTheme.SelectedItem is ComboBoxItem selected)
            {
                string theme = selected.Tag.ToString(); // "dark" or "light"

                var dict = new ResourceDictionary();
                dict.Source = new Uri($"Themes/{theme.Substring(0, 1).ToUpper() + theme.Substring(1)}Theme.xaml", UriKind.Relative);

                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(dict);

                // Optional: save to settings
                Settings.Default.InterfaceTheme = theme;
                Settings.Default.Save();
            }
        }
        

    }
}
