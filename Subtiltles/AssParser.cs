using NMH_Media_Player.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NMH_Media_Player.Subtiltles
{
    public class AssParser : ISubtitleParser
    {
        public List<SubtitleEntry> Parse(string filePath)
        {
            var entries = new List<SubtitleEntry>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (!line.StartsWith("Dialogue:"))
                    continue;

                string[] parts = line.Split(',');

                if (parts.Length < 10) continue;

                TimeSpan start = TimeSpan.ParseExact(parts[1], @"h\:mm\:ss\.ff", null);
                TimeSpan end = TimeSpan.ParseExact(parts[2], @"h\:mm\:ss\.ff", null);

                string text = string.Join(",", parts.Skip(9))
                                    .Replace("\\N", "\n")
                                    .Replace("{\\i1}", "")
                                    .Replace("{\\i0}", "")
                                    .Replace("{\\b1}", "")
                                    .Replace("{\\b0}", "");

                entries.Add(new SubtitleEntry
                {
                    StartTime = start,
                    EndTime = end,
                    Text = text
                });
            }

            return entries;
        }
    }

}
