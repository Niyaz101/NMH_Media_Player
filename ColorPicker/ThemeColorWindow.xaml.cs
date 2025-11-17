using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace NMH_Media_Player.ColorPicker
{
    public partial class ThemeColorWindow : Window
    {
        public Color SelectedColor { get; private set; }
        private bool isUpdating = false;
        private double brightness = 1.0;

        public ThemeColorWindow()
        {
            try
            {
                InitializeComponent();

                // Load saved color safely
                var color = ThemeManager.LoadColor();
                SetColor(color);
                SliderBrightness.Value = 1.0;
            }
            catch (Exception ex)
            {
                LogError("Error initializing ThemeColorWindow", ex);
                System.Windows.MessageBox.Show("Theme color UI failed to initialize properly.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (isUpdating) return;

                var color = Color.FromRgb(
                    (byte)SliderR.Value,
                    (byte)SliderG.Value,
                    (byte)SliderB.Value
                );
                UpdateFromColor(color);
            }
            catch (Exception ex)
            {
                LogError("Error while adjusting RGB sliders", ex);
            }
        }

        private void TxtHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;

            try
            {
                var hex = TxtHex.Text.TrimStart('#');
                if (hex.Length == 6)
                {
                    var color = (Color)ColorConverter.ConvertFromString("#" + hex);
                    SetColor(color);
                }
            }
            catch (Exception ex)
            {
                LogError("Invalid HEX color input", ex);
            }
        }

        private void ColorPickerControl_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            try
            {
                if (isUpdating || e.NewValue == null) return;
                SetColor(e.NewValue.Value);
            }
            catch (Exception ex)
            {
                LogError("Error in ColorPickerControl selection", ex);
            }
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (isUpdating) return;

                brightness = e.NewValue;
                var baseColor = Color.FromRgb(
                    (byte)SliderR.Value,
                    (byte)SliderG.Value,
                    (byte)SliderB.Value
                );

                var adjusted = AdjustBrightness(baseColor, brightness);
                UpdateFromColor(adjusted);
            }
            catch (Exception ex)
            {
                LogError("Error while changing brightness", ex);
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThemeManager.ApplyTheme(Application.Current.MainWindow, SelectedColor);
                ThemeManager.SaveColor(SelectedColor);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LogError("Failed to apply or save theme", ex);
                System.Windows.MessageBox.Show("Unable to apply theme. Please try again.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                LogError("Error while closing window", ex);
            }
        }

        // ===== Helpers =====
        public void SetColor(Color color)
        {
            try
            {
                isUpdating = true;

                SliderR.Value = color.R;
                SliderG.Value = color.G;
                SliderB.Value = color.B;

                TxtR.Text = color.R.ToString();
                TxtG.Text = color.G.ToString();
                TxtB.Text = color.B.ToString();

                TxtHex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                ColorPickerControl.SelectedColor = color;
                PreviewBox.Background = new SolidColorBrush(color);

                ThemeManager.ApplyTheme(Application.Current.MainWindow, color);
                ThemeManager.SaveColor(color);
                SelectedColor = color;
            }
            catch (Exception ex)
            {
                LogError("Error in SetColor()", ex);
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void UpdateFromColor(Color color)
        {
            try
            {
                if (TxtR == null || TxtG == null || TxtB == null || TxtHex == null)
                    return;

                TxtR.Text = color.R.ToString();
                TxtG.Text = color.G.ToString();
                TxtB.Text = color.B.ToString();
                TxtHex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                if (FindName("ColorPreviewBrush") is SolidColorBrush brush)
                    brush.Color = color;

                SelectedColor = color;

                ThemeManager.ApplyTheme(Application.Current.MainWindow, color);
                ThemeManager.SaveColor(color);
            }
            catch (Exception ex)
            {
                LogError("Error in UpdateFromColor()", ex);
            }
        }

        private Color AdjustBrightness(Color color, double factor)
        {
            try
            {
                byte r = (byte)Math.Min(255, color.R * factor);
                byte g = (byte)Math.Min(255, color.G * factor);
                byte b = (byte)Math.Min(255, color.B * factor);
                return Color.FromRgb(r, g, b);
            }
            catch (Exception ex)
            {
                LogError("Error adjusting brightness", ex);
                return color; // return original to stay safe
            }
        }

        private void LogError(string context, Exception ex)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThemeError.log");
                File.AppendAllText(path, $"[{DateTime.Now}] {context}: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch
            {
                // ignore logging errors
            }
        }
    }
}
