using NMH_Media_Player.Modules;

public interface ISubtitleParser
{
    List<SubtitleEntry> Parse(string filePath);
}
