using NMH_Media_Player.Subtitles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NMH_Media_Player.Subtitles
{
    public static class EmbeddedSubtitleDetector
    {
        public static List<EmbeddedSubtitleTrack> GetEmbeddedSubtitles(string filePath)
        {
            var tracks = new List<EmbeddedSubtitleTrack>();

            if (!File.Exists(filePath))
            {
                Debug.WriteLine("❌ File does not exist for subtitle detection");
                return tracks;
            }

            try
            {
                Debug.WriteLine($"🔍 Starting advanced subtitle detection for: {Path.GetFileName(filePath)}");

                // Method 1: Try ffprobe with JSON output (most reliable)
                tracks = DetectWithFFprobeJSON(filePath);

                // Method 2: If that fails, try direct stream inspection
                if (tracks.Count == 0)
                {
                    Debug.WriteLine("🔄 Trying direct stream inspection...");
                    tracks = DetectWithStreamInspection(filePath);
                }

                Debug.WriteLine($"✅ Found {tracks.Count} embedded subtitle tracks");
                foreach (var track in tracks)
                {
                    Debug.WriteLine($"   - Track {track.TrackIndex}: {track.Language} [{track.Codec}] '{track.Name}' (Default: {track.IsDefault})");
                }

                return tracks;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Error detecting embedded subtitles: {ex.Message}");
                return tracks;
            }
        }

        private static List<EmbeddedSubtitleTrack> DetectWithFFprobeJSON(string filePath)
        {
            var tracks = new List<EmbeddedSubtitleTrack>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v quiet -print_format json -show_streams \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc == null) return tracks;

                    string jsonOutput = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(10000);

                    Debug.WriteLine($"FFprobe JSON exit code: {proc.ExitCode}");

                    if (proc.ExitCode == 0 && !string.IsNullOrEmpty(jsonOutput))
                    {
                        tracks = ParseJSONOutput(jsonOutput);
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
                        Debug.WriteLine($"FFprobe JSON error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 JSON detection failed: {ex.Message}");
            }

            return tracks;
        }

        private static List<EmbeddedSubtitleTrack> ParseJSONOutput(string jsonOutput)
        {
            var tracks = new List<EmbeddedSubtitleTrack>();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonOutput);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("streams", out JsonElement streams))
                {
                    int subtitleIndex = 0;
                    foreach (JsonElement stream in streams.EnumerateArray())
                    {
                        if (stream.TryGetProperty("codec_type", out JsonElement codecType) &&
                            codecType.GetString() == "subtitle")
                        {
                            // Extract basic info
                            string codecName = stream.TryGetProperty("codec_name", out JsonElement codecNameElem)
                                ? codecNameElem.GetString() ?? "unknown"
                                : "unknown";

                            // Extract language from tags
                            string language = "Unknown";
                            string title = $"Subtitle {subtitleIndex}";
                            bool isDefault = false;

                            if (stream.TryGetProperty("tags", out JsonElement tags))
                            {
                                language = tags.TryGetProperty("language", out JsonElement langElem)
                                    ? langElem.GetString() ?? "Unknown"
                                    : "Unknown";

                                title = tags.TryGetProperty("title", out JsonElement titleElem)
                                    ? titleElem.GetString() ?? $"Subtitle {subtitleIndex}"
                                    : $"Subtitle {subtitleIndex}";

                                // Handle hearing impaired tags
                                if (title.Contains("hearing", StringComparison.OrdinalIgnoreCase) ||
                                    title.Contains("SDH", StringComparison.OrdinalIgnoreCase))
                                {
                                    title += " [SDH]";
                                }
                            }

                            // Extract disposition (default track)
                            if (stream.TryGetProperty("disposition", out JsonElement disposition))
                            {
                                isDefault = disposition.TryGetProperty("default", out JsonElement defaultElem) &&
                                           defaultElem.GetInt32() == 1;
                            }

                            // Convert language code to readable name
                            string languageName = GetLanguageName(language);

                            tracks.Add(new EmbeddedSubtitleTrack
                            {
                                TrackIndex = subtitleIndex++,
                                Codec = codecName,
                                Language = languageName,
                                Name = title,
                                IsDefault = isDefault
                            });

                            Debug.WriteLine($"📝 JSON Track: Lang='{language}'->'{languageName}', Title='{title}', Default={isDefault}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 JSON parsing failed: {ex.Message}");
            }

            return tracks;
        }

        private static List<EmbeddedSubtitleTrack> DetectWithStreamInspection(string filePath)
        {
            var tracks = new List<EmbeddedSubtitleTrack>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -select_streams s -show_entries stream=index,codec_name,codec_long_name:stream_tags=language,title:stream_disposition=default -of csv=p=0 \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc == null) return tracks;

                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(10000);

                    Debug.WriteLine($"FFprobe CSV exit code: {proc.ExitCode}");
                    Debug.WriteLine($"FFprobe CSV output: '{output}'");

                    if (proc.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        tracks = ParseCSVOutput(output);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Stream inspection failed: {ex.Message}");
            }

            return tracks;
        }

        private static List<EmbeddedSubtitleTrack> ParseCSVOutput(string output)
        {
            var tracks = new List<EmbeddedSubtitleTrack>();
            int trackIndex = 0;

            try
            {
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    // Expected format: index,codec_name,codec_long_name,language,title,default
                    string[] parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        string codec = parts[1].Trim('"').Trim();
                        string language = parts.Length > 3 ? parts[3].Trim('"').Trim() : "und";
                        string title = parts.Length > 4 ? parts[4].Trim('"').Trim() : $"Subtitle {trackIndex}";
                        bool isDefault = parts.Length > 5 && parts[5].Trim('"').Trim() == "1";

                        // Clean up values
                        language = string.IsNullOrEmpty(language) || language == "und" ? "Unknown" : language;
                        title = string.IsNullOrEmpty(title) ? $"Subtitle {trackIndex}" : title;

                        // Handle hearing impaired tags
                        if (title.Contains("hearing", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("SDH", StringComparison.OrdinalIgnoreCase))
                        {
                            title += " [SDH]";
                        }

                        // Convert language code to readable name
                        string languageName = GetLanguageName(language);

                        tracks.Add(new EmbeddedSubtitleTrack
                        {
                            TrackIndex = trackIndex++,
                            Codec = codec,
                            Language = languageName,
                            Name = title,
                            IsDefault = isDefault
                        });

                        Debug.WriteLine($"📝 CSV Track: Lang='{language}'->'{languageName}', Title='{title}', Default={isDefault}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 CSV parsing failed: {ex.Message}");
            }

            return tracks;
        }

        private static string GetLanguageName(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == "und" || languageCode == "Unknown")
                return "Unknown";

            // Clean the language code
            languageCode = languageCode.Trim().ToLower();

            // Common language code mapping
            var languageMap = new Dictionary<string, string>
            {
                ["eng"] = "English",
                ["en"] = "English",
                ["en-us"] = "English",
                ["en-gb"] = "English",
                ["fr"] = "French",
                ["fre"] = "French",
                ["fra"] = "French",
                ["fr-fr"] = "French",
                ["es"] = "Spanish",
                ["spa"] = "Spanish",
                ["es-es"] = "Spanish",
                ["de"] = "German",
                ["ger"] = "German",
                ["deu"] = "German",
                ["de-de"] = "German",
                ["it"] = "Italian",
                ["ita"] = "Italian",
                ["it-it"] = "Italian",
                ["ja"] = "Japanese",
                ["jpn"] = "Japanese",
                ["ja-jp"] = "Japanese",
                ["ko"] = "Korean",
                ["kor"] = "Korean",
                ["ko-kr"] = "Korean",
                ["zh"] = "Chinese",
                ["chi"] = "Chinese",
                ["zho"] = "Chinese",
                ["zh-cn"] = "Chinese",
                ["ru"] = "Russian",
                ["rus"] = "Russian",
                ["ru-ru"] = "Russian",
                ["ar"] = "Arabic",
                ["ara"] = "Arabic",
                ["ar-sa"] = "Arabic",
                ["hi"] = "Hindi",
                ["hin"] = "Hindi",
                ["hi-in"] = "Hindi",
                ["pt"] = "Portuguese",
                ["por"] = "Portuguese",
                ["pt-br"] = "Portuguese",
                ["pt-pt"] = "Portuguese"
            };

            // Try exact match first
            if (languageMap.ContainsKey(languageCode))
                return languageMap[languageCode];

            // Try partial match (e.g., "eng" in "eng:English")
            foreach (var pair in languageMap)
            {
                if (languageCode.Contains(pair.Key))
                    return pair.Value;
            }

            // If it's already a readable name, return it as is
            if (languageCode.Length <= 3)
                return languageCode.ToUpper(); // Return codes like "PGS", "VOB" etc.
            else
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageCode); // Capitalize if it's a name
        }
    }
}