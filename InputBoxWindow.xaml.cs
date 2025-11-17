using NMH_Media_Player.ColorPicker;
using System.Windows;

namespace NMH_Media_Player
{
    public partial class InputBoxWindow : Window
    {
        public string InputText { get; private set; } = "";
        public string Prompt { get; private set; } = "";

        public InputBoxWindow(string title, string prompt)
        {
            InitializeComponent();
            try
            {
                // Load saved theme color
                var savedColor = ThemeManager.LoadColor();

                // Apply theme to this window
                ThemeManager.ApplyTheme(this, savedColor);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to apply theme:\n{ex.Message}",
                                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.Title = title;
            this.Prompt = prompt;
            this.DataContext = this;

            txtInput.Focus();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
