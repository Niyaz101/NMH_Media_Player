using System;
using System.IO;
using System.Linq;

namespace NMH_Media_Player.Modules.Playlists
{
    public class PlaylistTrack
    {
        private string _title = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        // Title: if not explicitly set, provide filename without extension
        public string Title
        {
            get => string.IsNullOrEmpty(_title) ? Path.GetFileNameWithoutExtension(FilePath) : _title;
            set => _title = value ?? string.Empty;
        }

        // Optional: full file name
        public string FileName => Path.GetFileName(FilePath);

        // Optional: Duration (can be filled later)
        public string Duration { get; set; } = "Unknown";

        // Identify whether it's audio or video
        public string Type
        {
            get
            {
                string ext = Path.GetExtension(FilePath).ToLowerInvariant();
                if (new[] { ".mp3", ".wav", ".aac", ".flac", ".wma" }.Contains(ext))
                    return "Audio";
                else if (new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv" }.Contains(ext))
                    return "Video";
                else
                    return "Unknown";
            }
        }
    }
}
