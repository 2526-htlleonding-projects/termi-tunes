namespace Core;

/// <summary>
/// The Song stores all data that is relevant for the music master.
/// </summary>
public class Song
{
    public string Title { get; }
    public string Artist { get; }
    public TimeSpan Duration { get; }
    public SongSource Source { get; }

    public Song(string title, string artist, TimeSpan duration, SongSource source)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
        Source = source;
    }
}

public enum SongSource
{
    Local,
    Spotify
}