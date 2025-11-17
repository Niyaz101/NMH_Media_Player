using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NMH_Media_Player
{
    public partial class PropertiesWindow : Window
    {
        public PropertiesWindow(JsonDocument metadata, string filePath, Window owner)
        {
            InitializeComponent();

            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.ResizeMode = ResizeMode.NoResize;
            this.Title = "Media Properties";

            BuildUI(metadata, filePath);
        }

        private void BuildUI(JsonDocument metadata, string filePath)
        {
            // Clear previous content
            MainGrid.Children.Clear();

            TabControl tabControl = new TabControl();

            // ===== General Tab =====
            TabItem generalTab = new TabItem { Header = "General" };
            StackPanel generalPanel = new StackPanel { Margin = new Thickness(10), Background = Brushes.White };

            AddProperty(generalPanel, "File", Path.GetFileName(filePath));
            AddProperty(generalPanel, "Path", filePath);

            if (metadata.RootElement.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("format_name", out var fmt))
                    AddProperty(generalPanel, "Format", fmt.GetString());

                if (format.TryGetProperty("duration", out var dur))
                    AddProperty(generalPanel, "Duration", TimeSpan.FromSeconds(ParseJsonDouble(dur)).ToString(@"hh\:mm\:ss"));

                if (format.TryGetProperty("size", out var size))
                    AddProperty(generalPanel, "Size", $"{(long)ParseJsonDouble(size) / 1024} KB");

                if (format.TryGetProperty("bit_rate", out var bitrate))
                    AddProperty(generalPanel, "Bitrate", $"{(long)ParseJsonDouble(bitrate) / 1000} kbps");
            }

            generalTab.Content = new ScrollViewer { Content = generalPanel };
            tabControl.Items.Add(generalTab);

            // ===== Streams Tabs (Audio / Video) =====
            if (metadata.RootElement.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    string codecType = stream.GetProperty("codec_type").GetString();

                    // Create header with icon + text
                    StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    Image icon = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0) };

                    try
                    {
                        if (codecType == "video")
                            icon.Source = new BitmapImage(new Uri("pack://application:,,,/NMH_Media_Player;component/Assets/Icons/video.png"));
                        else if (codecType == "audio")
                            icon.Source = new BitmapImage(new Uri("pack://application:,,,/NMH_Media_Player;component/Assets/Icons/audio.png"));
                    }
                    catch { icon = null; }

                    TextBlock txt = new TextBlock
                    {
                        Text = codecType.ToUpper(),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (icon != null) headerPanel.Children.Add(icon);
                    headerPanel.Children.Add(txt);

                    TabItem tab = new TabItem { Header = headerPanel };

                    // Create stream panel
                    StackPanel panel = new StackPanel
                    {
                        Margin = new Thickness(10),
                        Background = (codecType == "video") ? new SolidColorBrush(Color.FromRgb(235, 245, 255)) :
                                     (codecType == "audio") ? new SolidColorBrush(Color.FromRgb(245, 255, 235)) :
                                     Brushes.White
                    };

                    AddStreamProperties(panel, stream, codecType);

                    tab.Content = new ScrollViewer { Content = panel };
                    tabControl.Items.Add(tab);
                }
            }

            MainGrid.Children.Add(tabControl);
        }

        // Helper: Add stream-specific properties
        private void AddStreamProperties(StackPanel panel, JsonElement stream, string codecType)
        {
            if (stream.TryGetProperty("codec_name", out var codec))
                AddProperty(panel, "Codec", codec.GetString());

            if (codecType == "video")
            {
                if (stream.TryGetProperty("width", out var w))
                    AddProperty(panel, "Width", ((int)ParseJsonDouble(w)).ToString());

                if (stream.TryGetProperty("height", out var h))
                    AddProperty(panel, "Height", ((int)ParseJsonDouble(h)).ToString());

                if (stream.TryGetProperty("r_frame_rate", out var fps))
                    AddProperty(panel, "Frame Rate", fps.GetString() + " fps");

                if (stream.TryGetProperty("bit_rate", out var vbr))
                    AddProperty(panel, "Bitrate", ((long)ParseJsonDouble(vbr) / 1000).ToString() + " kbps");
            }
            else if (codecType == "audio")
            {
                if (stream.TryGetProperty("channels", out var ch))
                    AddProperty(panel, "Channels", ((int)ParseJsonDouble(ch)).ToString());

                if (stream.TryGetProperty("sample_rate", out var sr))
                    AddProperty(panel, "Sample Rate", ((int)ParseJsonDouble(sr)).ToString() + " Hz");

                if (stream.TryGetProperty("bit_rate", out var abr))
                    AddProperty(panel, "Bitrate", ((long)ParseJsonDouble(abr) / 1000).ToString() + " kbps");
            }
        }

        // Add property with border
        private void AddProperty(Panel panel, string label, string value)
        {
            Border border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Margin = new Thickness(2),
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(3),
                Background = Brushes.White
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = label + ": ",
                FontWeight = FontWeights.Bold,
                Width = 120
            });
            sp.Children.Add(new TextBlock { Text = value });

            border.Child = sp;
            panel.Children.Add(border);
        }

        // Safely parse JSON element
        private double ParseJsonDouble(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out double val))
                return val;
            if (element.ValueKind == JsonValueKind.Number)
                return element.GetDouble();
            return 0;
        }
    }
}
