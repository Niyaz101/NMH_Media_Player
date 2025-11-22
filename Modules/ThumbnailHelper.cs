


using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace NMH_Media_Player.Modules
{
    /// <summary>
    /// Helper class for generating thumbnail grids for videos.
    /// This class preserves original behavior and logic but adds:
    ///  - block-level XML documentation,
    ///  - clearer internal comments,
    ///  - more precise exception handling,
    ///  - a friendly, user-facing message when the system cannot combine large grids (4K/8K / high rows/cols).
    /// 
    /// NOTE: No functional changes have been made to the frame extraction or combining algorithm.
    /// </summary>
    public static class ThumbnailHelper
    {
        /// <summary>
        /// Save thumbnails for the specified video as a single combined grid image.
        /// The behavior and algorithm are preserved from the original implementation.
        /// </summary>
        /// <param name="videoPath">Full path to the video file.</param>
        /// <param name="progress">Optional progress reporter (0-100).</param>
        /// <param name="rows">Number of rows in the final grid.</param>
        /// <param name="cols">Number of columns in the final grid.</param>
        /// <param name="scale">Scale multiplier used for borders and pen sizes.</param>
        /// <returns>True if thumbnails are successfully created and saved; otherwise false.</returns>
        public static async Task<bool> SaveThumbnailsAsync(
            string videoPath,
            IProgress<double> progress = null,
            int rows = 4,
            int cols = 5,
            int scale = 1)
        {
            // Validate input file existence first (critical error)
            try
            {
                if (!File.Exists(videoPath))
                {
                    MessageBox.Show("Video file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Quick audio-file guard (non-critical -> warning)
                string ext = Path.GetExtension(videoPath).ToLower();
                string[] audioExtensions = { ".mp3", ".wav", ".aac", ".flac", ".wma" };
                if (Array.Exists(audioExtensions, e => e == ext))
                {
                    MessageBox.Show("Cannot create thumbnails for audio files.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Hardcoded ffmpeg/ffprobe locations (critical error if missing)
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                string ffprobePath = @"C:\ffmpeg\bin\ffprobe.exe";

                if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
                {
                    MessageBox.Show("FFmpeg or FFprobe not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string tempFolder = Path.Combine(Path.GetTempPath(), "NMH_Thumbnails");

                // Temp folder creation (critical error if cannot create)
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

                // Read video duration using ffprobe (critical error if fails)
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

                // Multithreaded extraction using ffmpeg (we wrap exceptions and continue where possible)
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
                                // Non-fatal: log and continue extracting other frames
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
                        // Non-critical: probably no frames extracted -> warning and exit
                        MessageBox.Show("No frames were extracted!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading extracted frames: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // ---------------------------------------------------------------------
                // Combine frames into a grid
                // ---------------------------------------------------------------------
                // IMPORTANT: Any exception that happens during grid creation (including
                // OutOfMemoryException, ExternalException (GDI+), or any other Exception)
                // will show a single friendly, actionable message (replacement of the
                // previous detailed exception text) that instructs the user to lower
                // rows/cols or scale. This is intentional per the requested behavior.
                // ---------------------------------------------------------------------
                try
                {
                    using (Bitmap first = new Bitmap(frames[0]))
                    {
                        int maxThumbWidth = 3000;
                        int maxThumbHeight = 3000;

                        double ratio = Math.Min(
                            (double)maxThumbWidth / first.Width,
                            (double)maxThumbHeight / first.Height
                        );

                        int thumbWidth = (int)(first.Width * ratio);
                        int thumbHeight = (int)(first.Height * ratio);

                        int border = 5 * scale;

                        int finalWidth = cols * thumbWidth + (cols + 1) * border;
                        int finalHeight = rows * thumbHeight + (rows + 1) * border;

                        // Attempt to allocate the final combined bitmap and draw.
                        // If anything fails here (memory, graphics, GDI+), we will show
                        // the friendly "system cannot combine" message and return false.
                        try
                        {
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
                                    catch (OutOfMemoryException)
                                    {
                                        // Replace the original technical message with a friendly one.
                                        ShowCombineGridResourceMessage(isError: true);
                                        return false;
                                    }
                                    catch (ExternalException)
                                    {
                                        // GDI+ related failures; present the same friendly message.
                                        ShowCombineGridResourceMessage(isError: true);
                                        return false;
                                    }
                                    catch (Exception)
                                    {
                                        // Any other exception while combining a single frame:
                                        // per request, show the friendly message (replace original).
                                        ShowCombineGridResourceMessage(isError: true);
                                        return false;
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
                                catch (UnauthorizedAccessException)
                                {
                                    MessageBox.Show("Cannot save the generated thumbnail.\nPermission denied! Try running the app as Administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return false;
                                }
                                catch (IOException ex)
                                {
                                    MessageBox.Show("Disk or file system error while saving the final image:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return false;
                                }
                                catch (OutOfMemoryException)
                                {
                                    // Saving the final image ran out of memory (critical)
                                    ShowCombineGridResourceMessage(isError: true);
                                    return false;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Unexpected error while saving the final image:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return false;
                                }

                                return true;
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            // Allocation for the combined bitmap failed
                            ShowCombineGridResourceMessage(isError: true);
                            return false;
                        }
                        catch (ExternalException)
                        {
                            // Graphics subsystem / GDI+ failure allocating or creating Graphics
                            ShowCombineGridResourceMessage(isError: true);
                            return false;
                        }
                        catch (Exception)
                        {
                            // Any other error inside the overall combine block -> friendly message
                            ShowCombineGridResourceMessage(isError: true);
                            return false;
                        }
                    }
                }
                catch (OutOfMemoryException)
                {
                    ShowCombineGridResourceMessage(isError: true);
                    return false;
                }
                catch (ExternalException)
                {
                    ShowCombineGridResourceMessage(isError: true);
                    return false;
                }
                catch (Exception)
                {
                    ShowCombineGridResourceMessage(isError: true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Top-level unexpected error -> treat as critical
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                // Cleanup temporary thumbnails folder. Cleanup errors are ignored (non-critical).
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "NMH_Thumbnails");
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch
                {
                    // Intentionally ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Get the duration of the video (in seconds) using ffprobe.
        /// Returns 0 on failure.
        /// </summary>
        /// <param name="videoPath">Path to video file.</param>
        /// <param name="ffprobePath">Path to ffprobe executable.</param>
        /// <returns>Duration in seconds, or 0 if failed to read/parse.</returns>
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

        #region Private helper messages

        /// <summary>
        /// Centralized friendly message shown when combining frames into the grid fails
        /// due to resource limitations or other GDI/graphics failures.
        /// This method replaces the original technical exception details per request.
        /// </summary>
        /// <param name="isError">
        /// If true => show MessageBoxImage.Error (critical).
        /// If false => show MessageBoxImage.Warning (non-critical).
        /// </param>
        private static void ShowCombineGridResourceMessage(bool isError)
        {
            string msg =
                "Your system could not combine the generated frames into a thumbnail grid.\n\n" +
                "This usually happens when the resolution or grid size is too high (4K/8K) or your PC does not have enough RAM or GPU resources.\n\n" +
                "Try lowering the number of rows/columns or reducing the scale value.";

            MessageBoxImage icon = isError ? MessageBoxImage.Error : MessageBoxImage.Warning;
            MessageBox.Show(msg, "System Resources", MessageBoxButton.OK, icon);
        }

        #endregion
    }
}
