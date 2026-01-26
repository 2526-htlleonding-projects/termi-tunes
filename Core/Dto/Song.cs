using Core.Exceptions;

namespace Core.Dto;

/// <summary>
/// The Song is a DTO (Data Transfer Object) and just stores information.
/// </summary>
public sealed class Song
{
    public string Id { get; }
    public string Title { get; }
    public string Artist { get; }
    public TimeSpan Duration { get; }
    public SongSource Source { get; }
    public string SourcePath { get; }
    public string Nickname { get; }
    public List<Artist> Creators { get; }

    /// <summary>
    /// Constructor for Local Songs.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="source"></param>
    /// <param name="sourcePath"></param>
    /// <param name="nickname"></param>
    /// <param name="creators"></param>
    /// <exception cref="InvalidSongParameterException"></exception>
    public Song(string id, string title, string artist, TimeSpan duration, SongSource source, string? sourcePath, string nickname, List<Artist> creators)
    {
        //Song(id, cleanTitle, cleanArtist, duration, SongSource.Local, path, nickname)
        Id = id;
        Title = title.Trim();
        Artist = artist.Trim();
        Duration = duration;
        Source = source;
        SourcePath = sourcePath.Trim();
        Nickname = nickname;
        Creators = creators;
    }

    public override bool Equals(object? obj)
        => obj is Song other && Source == other.Source && Id == other.Id;

    public override int GetHashCode()
        => HashCode.Combine(Source, Id);
}

public enum SongSource
{
    Local,
    Spotify,
    Nigga
}