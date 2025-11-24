using NMH_Media_Player.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NMH_Media_Player.Subtitles
{
    public class MicroDvdParser : ISubtitleParser
    {
        public List<SubtitleEntry> Parse(string filePath)
        {
            var entries = new List<SubtitleEntry>();
            var lines = File.ReadAllLines(filePath);

            double frameRate = 25.0; // You can get real framerate from the video

            foreach (var line in lines)
            {
                // Format: {start}{end}Text
                var match = Regex.Match(line, @"\{(\d+)\}\{(\d+)\}(.*)");

                if (!match.Success)
                    continue;

                int startFrame = int.Parse(match.Groups[1].Value);
                int endFrame = int.Parse(match.Groups[2].Value);
                string text = match.Groups[3].Value.Trim();

                entries.Add(new SubtitleEntry
                {
                    StartTime = TimeSpan.FromSeconds(startFrame / frameRate),
                    EndTime = TimeSpan.FromSeconds(endFrame / frameRate),
                    Text = text
                });
            }

            return entries;
        }
    }

}
