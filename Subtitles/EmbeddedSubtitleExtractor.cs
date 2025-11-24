using NMH_Media_Player.Modules;
using NMH_Media_Player.Subtitles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NMH_Media_Player.Subtitles
{
    public static class EmbeddedSubtitleExtractor
    {
        /// <summary>
        /// Extracts embedded subtitles from a video file using FFmpeg
        /// and returns them as a list of SubtitleEntry.
        /// </summary>
        /// <param name="track">The embedded subtitle track</param>
        /// <param name="videoFile">Path to video file</param>
        /// <returns>List of SubtitleEntry</returns>
        public static List<SubtitleEntry> Extract(EmbeddedSubtitleTrack track, string videoFile)
        {
            var result = new List<SubtitleEntry>();

            if (!File.Exists(videoFile))
                throw new FileNotFoundException($"Video file not found: {videoFile}");

            // Create a temporary file for extracted subtitles
            string tempSrt = Path.Combine(Path.GetTempPath(), $"embedded_{Guid.NewGuid()}.srt");

            try
            {
                // FFmpeg command to extract subtitle track by index
                string ffmpegArgs = $"-i \"{videoFile}\" -map 0:s:{track.TrackIndex} \"{tempSrt}\" -y";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe", // Ensure ffmpeg is in PATH
                    Arguments = ffmpegArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();

                    // Optional: read FFmpeg output for debugging
                    string output = process.StandardError.ReadToEnd();
                    // Console.WriteLine(output);
                }

                // Load the extracted SRT into SubtitleEntry list
                if (File.Exists(tempSrt))
                {
                    SubtitleManager manager = new SubtitleManager();
                    result = manager.Load(tempSrt);

                    // Clean up temp file
                    File.Delete(tempSrt);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract embedded subtitles: {ex.Message}");
            }

            return result;
        }
    }
}

