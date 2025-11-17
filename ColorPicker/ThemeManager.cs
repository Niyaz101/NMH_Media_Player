using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NMH_Media_Player.ColorPicker
{
    public static class ThemeManager
    {
        public static Color CurrentColor { get; private set; } = Colors.DimGray;

        private static string ThemeFile
        {
            get
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                          "NMH_Media_Player");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "theme.txt");
            }
        }

        // Apply theme to MainWindow using a base color
        public static void ApplyTheme(Window window, Color baseColor)
        {
            if (window == null) return;

            var accentBrush = new SolidColorBrush(baseColor);
            var lighterBrush = new SolidColorBrush(Lighten(baseColor, 0.3));
            var darkerBrush = new SolidColorBrush(Darken(baseColor, 0.2));

            // Set window background
            window.Background = new SolidColorBrush(Darken(baseColor, 0.05));

            // Recursively style all child controls
            ApplyThemeToChildren(window, accentBrush, lighterBrush, darkerBrush);
        }

        private static void ApplyThemeToChildren(DependencyObject parent, SolidColorBrush accent, SolidColorBrush light, SolidColorBrush dark)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // ================= Skip Certain Areas =================
                // Skip playback area and its contents
                if (child is Border border && border.Name == "PlaybackBorder")
                    continue;


                if (child is Canvas canvas && canvas.Name == "VisualizerCanvas")
                    continue;

                if (child is MediaElement)
                    continue;

                if (child is WrapPanel wrap && wrap.Name == "ControlBarPanel")
                    continue;

                if (child is Slider sldr && sldr.Name == "volumeSlider")
                    continue;

                // Skip Min/Max/Close buttons by checking their Click event
                // Skip Min/Max/Close buttons by their Content and size
                if (child is Button button)
                {
                    if ((button.Width == 35 && button.Height == 30) &&
                        (button.Content?.ToString() == "─" ||
                         button.Content?.ToString() == "▢" ||
                         button.Content?.ToString() == "X"))
                    {
                        continue; // skip coloring these buttons
                    }

                    // Apply theme to other buttons
                    button.Background = accent;
                    button.BorderBrush = light;
                    button.Foreground = light;
                }


                // ================= Menu Items =================
                if (child is MenuItem menuItem)
                {
                    // Apply background to all MenuItems
                    menuItem.Background = accent;

                    // Determine if it is a top-level menu (direct child of Menu)
                    bool isTopLevel = VisualTreeHelper.GetParent(menuItem) is Menu;

                    // Apply Foreground only to non-top-level menus
                    if (!isTopLevel)
                    {
                        menuItem.Foreground = light;
                    }

                    // Special handling for ThemeColorMenu
                    if (menuItem.Name == "ThemeColorMenu")
                    {
                        menuItem.Foreground = Brushes.White;
                        ApplyThemeToChildren(menuItem, accent, Brushes.White, dark);
                        return;
                    }

                    // Fix top-level text: find the HeaderHost ContentPresenter inside the template
                    if (isTopLevel)
                    {
                        menuItem.Loaded += (s, e) =>
                        {
                            // Walk visual tree of the MenuItem
                            SetHeaderForeground(menuItem, Brushes.White);
                        };
                    }


                    // Helper method






                    // ================= Other Controls =================
                    switch (child)
                    {
                        case Button btnElement:
                            btnElement.Background = accent;
                            btnElement.BorderBrush = light;
                            btnElement.Foreground = light;
                            break;

                        case TextBlock txtBlock:
                            txtBlock.Foreground = light;
                            break;

                        case Label lblElement:
                            lblElement.Foreground = light;
                            break;

                        case Panel panelElement:
                            panelElement.Background = new SolidColorBrush(Darken(accent.Color, 0.1));
                            break;

                        case Border borderElement:
                            borderElement.Background = new SolidColorBrush(Darken(accent.Color, 0.1));
                            break;

                        case TextBox tbElement:
                            tbElement.Background = new SolidColorBrush(Darken(accent.Color, 0.1));
                            tbElement.Foreground = light;
                            break;

                        case Slider sliderElement:
                            sliderElement.Background = new SolidColorBrush(Darken(accent.Color, 0.2));
                            break;
                    }

                    // Recursive call
                    ApplyThemeToChildren(child, accent, light, dark);
                }
            }
        }


        private static void SetHeaderForeground(DependencyObject parent, Brush foreground)
                {
                    int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
                    for (int i = 0; i < childrenCount; i++)
                    {
                        var child = VisualTreeHelper.GetChild(parent, i);

                        if (child is TextBlock txt)
                        {
                            txt.Foreground = foreground;
                        }
                        else
                        {
                            SetHeaderForeground(child, foreground); // recursive search
                        }
                    }
                }

        public static void SaveColor(Color color)
        {
            try
            {
                File.WriteAllText(ThemeFile, $"{color.R},{color.G},{color.B}");
            }
            catch { }
        }

        public static Color LoadColor()
        {
            try
            {
                if (File.Exists(ThemeFile))
                {
                    var parts = File.ReadAllText(ThemeFile).Split(',');
                    if (parts.Length == 3 &&
                        byte.TryParse(parts[0], out byte r) &&
                        byte.TryParse(parts[1], out byte g) &&
                        byte.TryParse(parts[2], out byte b))
                    {
                        return Color.FromRgb(r, g, b);
                    }
                }
            }
            catch { }

            return Colors.DimGray;
        }

        private static Color Darken(Color color, double amount)
        {
            return Color.FromRgb(
                (byte)(color.R * (1 - amount)),
                (byte)(color.G * (1 - amount)),
                (byte)(color.B * (1 - amount))
            );
        }

        private static Color Lighten(Color color, double amount)
        {
            return Color.FromRgb(
                (byte)(color.R + (255 - color.R) * amount),
                (byte)(color.G + (255 - color.G) * amount),
                (byte)(color.B + (255 - color.B) * amount)
            );
        }
    }
}
