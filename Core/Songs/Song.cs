using System.Text;
using Core.Exceptions;

namespace Core;

/// <summary>
/// The Song stores all data that is relevant for the music master.
/// </summary>
public sealed class Song
{
    public string Id { get; }
    public string Title { get; }
    public string Artist { get; }
    public TimeSpan Duration { get; }
    public SongSource Source { get; }

    public Song(string title, string artist, TimeSpan duration, SongSource source, string id = "")
    {
        Title = title.Trim();
        Artist = artist.Trim();
        Duration = duration;
        Source = source;

        if (source == SongSource.Local)
        {
            var rawId = $"{Artist}|{Title}|{Duration}";
            Id = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(rawId)));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(id)) 
                throw new InvalidSongParameterException("id");
            Id = id;
        }
    }

    public override bool Equals(object? obj)
        => obj is Song other && Source == other.Source && Id == other.Id;

    public override int GetHashCode()
        => HashCode.Combine(Source, Id);
}

public enum SongSource
{
    Local,
    Spotify
}