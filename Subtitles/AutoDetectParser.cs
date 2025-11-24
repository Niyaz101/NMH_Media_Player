using NMH_Media_Player.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NMH_Media_Player.Subtitles
{
    public class AutoDetectParser : ISubtitleParser
    {
        public List<SubtitleEntry> Parse(string filePath)
        {
            var firstLines = File.ReadLines(filePath).Take(5).ToList();

            if (firstLines.Any(l => l.Contains("-->")))
            {
                if (firstLines.Any(l => l.Contains(".")))
                    return new VttParser().Parse(filePath);

                return new SrtParser().Parse(filePath);
            }

            if (firstLines.Any(l => Regex.IsMatch(l, @"\{\d+\}\{\d+\}")))
                return new MicroDvdParser().Parse(filePath);


            if (firstLines.Any(l => l.StartsWith("Dialogue:")))
                return new AssParser().Parse(filePath);

            throw new Exception("Unknown subtitle format.");
        }
    }

}
