using NMH_Media_Player.Modules;
using NMH_Media_Player.Subtitles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NMH_Media_Player.Subtitles
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


        public List<EmbeddedSubtitleTrack> LoadEmbedded(string videoFilePath)
        {
            if (!System.IO.File.Exists(videoFilePath))
                throw new FileNotFoundException($"Video file not found: {videoFilePath}");

            try
            {
                // Call the detector correctly
                return EmbeddedSubtitleDetector.GetEmbeddedSubtitles(videoFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load embedded subtitles: " + ex.Message);
            }
        }


    }
}
