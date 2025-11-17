using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NMH_Media_Player.Modules.Handlers
{
    public static class FFmpegStatisticHelper
    {
        private static readonly string ffprobePath = @"C:\ffmpeg\bin\ffprobe.exe"; // adjust as needed

        public static (string Resolution, string FPS, string Bitrate) GetVideoStatistics(string videoPath)
        {
            if (!File.Exists(videoPath)) return ("N/A", "N/A", "N/A");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,r_frame_rate,bit_rate -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 4)
                {
                    string width = lines[0];
                    string height = lines[1];
                    string fpsRaw = lines[2]; // e.g., "30000/1001"
                    string bitrate = lines[3];

                    string resolution = $"{width}x{height}";
                    string fps = "N/A";

                    // calculate FPS
                    if (fpsRaw.Contains("/"))
                    {
                        string[] parts = fpsRaw.Split('/');
                        if (int.TryParse(parts[0], out int num) && int.TryParse(parts[1], out int den) && den != 0)
                            fps = (num / (double)den).ToString("F2");
                    }

                    if (int.TryParse(bitrate, out int br))
                        bitrate = $"{br / 1000} kbps";
                    else
                        bitrate = "N/A";

                    return (resolution, fps, bitrate);
                }
            }
            catch { }

            return ("N/A", "N/A", "N/A");
        }
    }
}
