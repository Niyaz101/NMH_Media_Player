using NMH_Media_Player.SettingsTabs; // Include your tabs namespace
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NMH_Media_Player.SettingsWindow
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // Load default tab (General)
            ContentArea.Content = new GeneralTab();

            // Highlight default button
            btnGeneral.Background = (Brush)new BrushConverter().ConvertFromString("#383838");
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ Reset all nav button backgrounds
            foreach (var btn in SidebarPanel.Children.OfType<Button>())
                btn.Background = Brushes.Transparent;

            // 2️⃣ Highlight the clicked button
            if (sender is Button clickedButton)
                clickedButton.Background = (Brush)new BrushConverter().ConvertFromString("#383838");

            // 3️⃣ Load corresponding content
            if (sender == btnGeneral)
                ContentArea.Content = new GeneralTab();
            else if (sender == btnInterface)
                ContentArea.Content = new InterfaceTab();
            else if (sender == btnSubtitles)
                ContentArea.Content = new SubtitlesTab();
            else if (sender == btnPlayback)
                ContentArea.Content = new PlaybackTab();
            else if (sender == btnAdvanced)
                ContentArea.Content = new AdvancedTab();
        }
    }
}
