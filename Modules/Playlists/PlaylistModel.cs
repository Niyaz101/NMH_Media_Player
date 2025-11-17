using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NMH_Media_Player.Modules.Playlists
{
    public class PlaylistModel
    {
        public string Name { get; set; } = "New Playlist";
        public List<PlaylistTrack> Tracks { get; set; } = new();

        private static string PlaylistDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NMH_Media_Player", "Playlists");

        public string FilePath => Path.Combine(PlaylistDir, $"{Name}.playlist");

        public void Save()
        {
            Directory.CreateDirectory(PlaylistDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static PlaylistModel Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PlaylistModel>(json) ?? new PlaylistModel();
        }

        public static void Delete(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
