using System.Windows;

namespace NMH_Media_Player.Modules.Handlers
{
    public static class NavigationEvents
    {
        public static void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            // Get MainWindow instance
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if (window?.mediaController != null)
            {
                // Use the MediaController's Forward method
                window.mediaController.Forward(10);
            }
            else
            {
                MessageBox.Show("No media is currently loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void BtnBackward_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            if (window?.mediaController != null)
            {
                window.mediaController.Rewind(10); // We'll add this method next
            }
            else
            {
                MessageBox.Show("No media is currently loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
    }
}
