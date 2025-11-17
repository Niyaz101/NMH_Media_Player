//using System;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Threading.Tasks;
//using System.Windows;

//namespace NMH_Media_Player.Modules
//{
//    public static class ThumbnailHelper
//    {
//        public static async Task SaveThumbnailsAsync(string videoPath, IProgress<double> progress = null, int rows = 4, int cols = 5, int scale = 1)
//        {
//            if (!File.Exists(videoPath))
//            {
//                MessageBox.Show("Video file not found!");
//                return;
//            }

//            string ext = Path.GetExtension(videoPath).ToLower();
//            string[] audioExtensions = { ".mp3", ".wav", ".aac", ".flac", ".wma" };
//            if (Array.Exists(audioExtensions, e => e == ext))
//            {
//                MessageBox.Show("Cannot create thumbnails for audio files.");
//                return;
//            }

//            string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
//            string ffprobePath = @"C:\ffmpeg\bin\ffprobe.exe";
//            if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
//            {
//                MessageBox.Show("FFmpeg or FFprobe not found!");
//                return;
//            }

//            string tempFolder = Path.Combine(Path.GetTempPath(), "NMH_Thumbnails");
//            Directory.CreateDirectory(tempFolder);

//            try
//            {
//                int totalFrames = rows * cols;
//                double duration = GetVideoDuration(videoPath, ffprobePath);

//                // Extract frames asynchronously
//                for (int i = 0; i < totalFrames; i++)
//                {
//                    double timestamp = (duration / (totalFrames + 1)) * (i + 1);
//                    string outputFrame = Path.Combine(tempFolder, $"thumb_{i}.jpg");

//                    string args = $"-ss {TimeSpan.FromSeconds(timestamp):hh\\:mm\\:ss} -accurate_seek -i \"{videoPath}\" -vframes 1 -q:v 2 -y \"{outputFrame}\"";

//                    await Task.Run(() =>
//                    {
//                        using Process process = new Process
//                        {
//                            StartInfo = new ProcessStartInfo
//                            {
//                                FileName = ffmpegPath,
//                                Arguments = args,
//                                UseShellExecute = false,
//                                CreateNoWindow = true
//                            }
//                        };
//                        process.Start();
//                        process.WaitForExit();
//                    });

//                    progress?.Report((i + 1) * 50.0 / totalFrames); // first 50% = frame extraction
//                }

//                // Combine frames into a grid
//                string[] frames = Directory.GetFiles(tempFolder, "thumb_*.jpg");
//                using (Bitmap first = new Bitmap(frames[0]))
//                {
//                    int thumbWidth = first.Width * scale;
//                    int thumbHeight = first.Height * scale;
//                    int border = 5 * scale;

//                    int finalWidth = cols * thumbWidth + (cols + 1) * border;
//                    int finalHeight = rows * thumbHeight + (rows + 1) * border;

//                    using (Bitmap combined = new Bitmap(finalWidth, finalHeight))
//                    using (Graphics g = Graphics.FromImage(combined))
//                    {
//                        g.Clear(Color.Black);

//                        for (int i = 0; i < frames.Length; i++)
//                        {
//                            int col = i % cols;
//                            int row = i / cols;
//                            int x = border + col * (thumbWidth + border);
//                            int y = border + row * (thumbHeight + border);

//                            using (Bitmap bmp = new Bitmap(frames[i]))
//                            using (Bitmap scaledBmp = new Bitmap(bmp, thumbWidth, thumbHeight))
//                            {
//                                g.DrawImage(scaledBmp, x, y, thumbWidth, thumbHeight);

//                                // Draw border
//                                using (Pen pen = new Pen(Color.White, 2 * scale))
//                                {
//                                    g.DrawRectangle(pen, x, y, thumbWidth, thumbHeight);
//                                }

//                                // Draw timestamp with shadow
//                                double timestampSeconds = (duration / (frames.Length + 1)) * (i + 1);
//                                TimeSpan ts = TimeSpan.FromSeconds(timestampSeconds);
//                                string timeText = ts.ToString(@"hh\:mm\:ss");

//                                using (Font font = new Font("Arial", 14 * scale, System.Drawing.FontStyle.Bold))
//                                using (Brush shadow = new SolidBrush(Color.Black))
//                                using (Brush brush = new SolidBrush(Color.Yellow))
//                                {
//                                    g.DrawString(timeText, font, shadow, x + 6, y + 6);
//                                    g.DrawString(timeText, font, brush, x + 5, y + 5);
//                                }
//                            }

//                            progress?.Report(50.0 + (i + 1) * 50.0 / frames.Length); // second 50% = grid drawing
//                        }

//                        string finalPath = Path.Combine(
//                            Path.GetDirectoryName(videoPath),
//                            Path.GetFileNameWithoutExtension(videoPath) + "_thumbnails.jpg"
//                        );

//                        combined.Save(finalPath, ImageFormat.Jpeg);
//                        MessageBox.Show($"Thumbnails saved to {finalPath}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Error creating thumbnails: " + ex.Message);
//            }
//            finally
//            {
//                try
//                {
//                    Directory.Delete(tempFolder, true);
//                }
//                catch { }
//            }
//        }

//        private static double GetVideoDuration(string videoPath, string ffprobePath)
//        {
//            ProcessStartInfo psi = new ProcessStartInfo
//            {
//                FileName = ffprobePath,
//                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
//                RedirectStandardOutput = true,
//                UseShellExecute = false,
//                CreateNoWindow = true
//            };

//            using (Process process = Process.Start(psi))
//            {
//                string result = process.StandardOutput.ReadToEnd();
//                process.WaitForExit();
//                if (double.TryParse(result, out double duration))
//                    return duration;
//                return 0;
//            }
//        }
//    }
//}




using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NMH_Media_Player.Modules
{
    public static class ThumbnailHelper
    {
        public static async Task<bool> SaveThumbnailsAsync(
            string videoPath,
            IProgress<double> progress = null,
            int rows = 4,
            int cols = 5,
            int scale = 1)
        {
            try
            {
                if (!File.Exists(videoPath))
                {
                    MessageBox.Show("Video file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string ext = Path.GetExtension(videoPath).ToLower();
                string[] audioExtensions = { ".mp3", ".wav", ".aac", ".flac", ".wma" };
                if (Array.Exists(audioExtensions, e => e == ext))
                {
                    MessageBox.Show("Cannot create thumbnails for audio files.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                string ffprobePath = @"C:\ffmpeg\bin\ffprobe.exe";

                if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
                {
                    MessageBox.Show("FFmpeg or FFprobe not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string tempFolder = Path.Combine(Path.GetTempPath(), "NMH_Thumbnails");

                try
                {
                    Directory.CreateDirectory(tempFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to create temp folder: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                int totalFrames = rows * cols;
                double duration = 0;

                try
                {
                    duration = GetVideoDuration(videoPath, ffprobePath);
                    if (duration <= 0)
                    {
                        MessageBox.Show("Cannot read video duration!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading video duration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Multithreaded extraction
                try
                {
                    await Task.Run(() =>
                    {
                        Parallel.For(0, totalFrames, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                        {
                            try
                            {
                                double timestamp = (duration / (totalFrames + 1)) * (i + 1);
                                string outputFrame = Path.Combine(tempFolder, $"thumb_{i}.jpg");

                                string args = $"-ss {TimeSpan.FromSeconds(timestamp):hh\\:mm\\:ss} -accurate_seek -i \"{videoPath}\" -vframes 1 -q:v 2 -y \"{outputFrame}\"";

                                using (Process process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = ffmpegPath,
                                        Arguments = args,
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    }
                                })
                                {
                                    process.Start();
                                    process.WaitForExit();
                                }

                                progress?.Report((i + 1) * 100.0 / totalFrames);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error extracting frame {i}: {ex.Message}");
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during frame extraction: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string[] frames = Array.Empty<string>();
                try
                {
                    frames = Directory.GetFiles(tempFolder, "thumb_*.jpg");
                    if (!frames.Any())
                    {
                        MessageBox.Show("No frames were extracted!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading extracted frames: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Combine frames into a grid
                try
                {
                    using (Bitmap first = new Bitmap(frames[0]))
                    {
                        int thumbWidth = first.Width * scale;
                        int thumbHeight = first.Height * scale;
                        int border = 5 * scale;

                        int finalWidth = cols * thumbWidth + (cols + 1) * border;
                        int finalHeight = rows * thumbHeight + (rows + 1) * border;

                        using (Bitmap combined = new Bitmap(finalWidth, finalHeight))
                        using (Graphics g = Graphics.FromImage(combined))
                        {
                            g.Clear(Color.Black);

                            for (int i = 0; i < frames.Length; i++)
                            {
                                try
                                {
                                    int col = i % cols;
                                    int row = i / cols;
                                    int x = border + col * (thumbWidth + border);
                                    int y = border + row * (thumbHeight + border);

                                    using (Bitmap bmp = new Bitmap(frames[i]))
                                    using (Bitmap scaledBmp = new Bitmap(bmp, thumbWidth, thumbHeight))
                                    {
                                        g.DrawImage(scaledBmp, x, y, thumbWidth, thumbHeight);

                                        using (Pen pen = new Pen(Color.White, 2 * scale))
                                            g.DrawRectangle(pen, x, y, thumbWidth, thumbHeight);

                                        double timestampSeconds = (duration / (frames.Length + 1)) * (i + 1);
                                        TimeSpan ts = TimeSpan.FromSeconds(timestampSeconds);
                                        string timeText = ts.ToString(@"hh\:mm\:ss");

                                        using (Font font = new Font("Arial", 30 * scale, System.Drawing.FontStyle.Bold))
                                        using (Brush shadow = new SolidBrush(Color.Black))
                                        using (Brush brush = new SolidBrush(Color.Yellow))
                                        {
                                            g.DrawString(timeText, font, shadow, x + 6, y + 6);
                                            g.DrawString(timeText, font, brush, x + 5, y + 5);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error combining frame {i}: {ex.Message}");
                                }
                            }

                            string finalPath = Path.Combine(
                                Path.GetDirectoryName(videoPath),
                                Path.GetFileNameWithoutExtension(videoPath) + "_thumbnails.jpg"
                            );

                            try
                            {
                                var encoder = ImageCodecInfo.GetImageEncoders()
                                                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);

                                if (encoder != null)
                                {
                                    var encoderParams = new EncoderParameters(1);
                                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                                    combined.Save(finalPath, encoder, encoderParams);
                                }
                                else
                                {
                                    combined.Save(finalPath, ImageFormat.Jpeg);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error saving final thumbnail image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return false;
                            }

                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error combining frames into final thumbnail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(Path.Combine(Path.GetTempPath(), "NMH_Thumbnails")))
                        Directory.Delete(Path.Combine(Path.GetTempPath(), "NMH_Thumbnails"), true);
                }
                catch { /* ignore cleanup errors */ }
            }
        }

        public static double GetVideoDuration(string videoPath, string ffprobePath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (double.TryParse(result, out double duration))
                        return duration;

                    MessageBox.Show("Failed to parse video duration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting video duration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }
    }
}
