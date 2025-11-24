using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NMH_Media_Player.Subtitles
{
    public static class EmbeddedSubtitleDetector
    {
        public static List<EmbeddedSubtitleTrack> GetEmbeddedSubtitles(string filePath)
        {
            var tracks = new List<EmbeddedSubtitleTrack>();

            if (!System.IO.File.Exists(filePath))
                return tracks;

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{filePath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                string output = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                // Regex to detect subtitle streams
                var regex = new Regex(@"Stream #\d+:(\d+)(\[\w+\])?: Subtitle: (\w+)(?: \((\w+)\))?");
                foreach (Match match in regex.Matches(output))
                {
                    int index = int.Parse(match.Groups[1].Value);
                    string codec = match.Groups[3].Value;
                    string language = match.Groups[4].Success ? match.Groups[4].Value : "Unknown";

                    tracks.Add(new EmbeddedSubtitleTrack
                    {
                        TrackIndex = index,
                        Codec = codec,
                        Language = language,
                        Name = $"Subtitle {index}",
                        IsDefault = tracks.Count == 0
                    });
                }
            }

            return tracks;
        }
    }
}
