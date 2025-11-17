using System;

namespace NMH_Media_Player.Modules
{
    public class SubtitleEntry
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
