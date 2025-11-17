using NMH_Media_Player.Properties; // For Settings
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinForms = System.Windows.Forms; // Only for FolderBrowserDialog

namespace NMH_Media_Player.SettingsTabs
{
    public partial class GeneralTab : UserControl
    {
        public GeneralTab()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load saved settings
            chkStartWithWindows.IsChecked = Settings.Default.StartWithWindows;
            chkResumeLastSession.IsChecked = Settings.Default.ResumeLastSession;
            chkShowSplash.IsChecked = Settings.Default.ShowSplash;
            txtVideoFolder.Text = Settings.Default.VideoFolder;
            txtAudioFolder.Text = Settings.Default.AudioFolder;
            chkShowNotifications.IsChecked = Settings.Default.ShowNotifications;
            sliderNotificationDuration.Value = Settings.Default.NotificationDuration;
            chkCheckUpdates.IsChecked = Settings.Default.CheckUpdates;
            chkEnableLogging.IsChecked = Settings.Default.EnableLogging;

            // Load language selection
            string lang = Settings.Default.Language;
            foreach (ComboBoxItem item in cmbLanguage.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == lang)
                {
                    cmbLanguage.SelectedItem = item;
                    break;
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Save settings
            Settings.Default.StartWithWindows = chkStartWithWindows.IsChecked ?? false;
            Settings.Default.ResumeLastSession = chkResumeLastSession.IsChecked ?? false;
            Settings.Default.ShowSplash = chkShowSplash.IsChecked ?? false;
            Settings.Default.VideoFolder = txtVideoFolder.Text;
            Settings.Default.AudioFolder = txtAudioFolder.Text;
            Settings.Default.ShowNotifications = chkShowNotifications.IsChecked ?? false;
            Settings.Default.NotificationDuration = (int)sliderNotificationDuration.Value;
            Settings.Default.CheckUpdates = chkCheckUpdates.IsChecked ?? false;
            Settings.Default.EnableLogging = chkEnableLogging.IsChecked ?? false;

            if (cmbLanguage.SelectedItem is ComboBoxItem selectedLang)
                Settings.Default.Language = selectedLang.Tag.ToString();

            Settings.Default.Save();

            MessageBox.Show("Settings saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            LoadSettings();
        }

        private void BrowseVideoFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WinForms.FolderBrowserDialog
            {
                Description = "Select Default Video Folder"
            };
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                txtVideoFolder.Text = dlg.SelectedPath;
        }

        private void BrowseAudioFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WinForms.FolderBrowserDialog
            {
                Description = "Select Default Audio Folder"
            };
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                txtAudioFolder.Text = dlg.SelectedPath;
        }



        



    }
}
