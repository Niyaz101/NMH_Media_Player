using NMH_Media_Player.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NMH_Media_Player.Subtiltles
{
    public class SubtitleManager
    {
        public List<SubtitleEntry> Load(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();

            ISubtitleParser parser = ext switch
            {
                ".srt" => new SrtParser(),
                ".vtt" => new VttParser(),
                ".ass" => new AssParser(),
                ".ssa" => new AssParser(),
                ".sub" => new MicroDvdParser(),
                _ => new AutoDetectParser()
            };

            return parser.Parse(filePath);
        }
    }

}
