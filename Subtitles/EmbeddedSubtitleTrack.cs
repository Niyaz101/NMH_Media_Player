using System;

namespace NMH_Media_Player.Subtitles
{
    public class EmbeddedSubtitleTrack
    {
        public int TrackIndex { get; set; }
        public string Language { get; set; } = "Unknown";
        public string Name { get; set; } = "Embedded Subtitle";
        public string Codec { get; set; } = "Unknown";
        public bool IsDefault { get; set; }
    }
}
