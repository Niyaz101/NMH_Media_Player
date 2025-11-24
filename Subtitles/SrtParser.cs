using NMH_Media_Player.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NMH_Media_Player.Subtitles
{
    public class SrtParser : ISubtitleParser
    {
        public List<SubtitleEntry> Parse(string filePath)
        {
            var entries = new List<SubtitleEntry>();
            var lines = File.ReadAllLines(filePath);
            SubtitleEntry entry = null;

            var timeRegex = new Regex(@"(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})");

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (entry != null)
                        entries.Add(entry);

                    entry = null;
                    continue;
                }

                if (int.TryParse(line.Trim(), out _)) continue;

                var match = timeRegex.Match(line);
                if (match.Success)
                {
                    entry = new SubtitleEntry
                    {
                        StartTime = TimeSpan.ParseExact(match.Groups[1].Value, @"hh\:mm\:ss\,fff", null),
                        EndTime = TimeSpan.ParseExact(match.Groups[2].Value, @"hh\:mm\:ss\,fff", null),
                        Text = ""
                    };
                }
                else if (entry != null)
                {
                    entry.Text += (entry.Text.Length > 0 ? "\n" : "") + line.Trim();
                }
            }

            if (entry != null)
                entries.Add(entry);

            return entries;
        }
    }

}
