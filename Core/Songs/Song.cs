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
    public string SourcePath { get; }

    /// <summary>
    /// Constructor for Local Songs.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="sourcePath"></param>
    /// <exception cref="InvalidSongParameterException"></exception>
    public Song(string title, string artist, TimeSpan duration, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new InvalidSongParameterException(nameof(sourcePath));

        Title = title.Trim();
        Artist = artist.Trim();
        Duration = duration;
        Source = SongSource.Local;
        SourcePath = sourcePath.Trim();

        // Generate deterministic ID from metadata
        var rawId = $"{Artist}|{Title}|{Duration}";
        Id = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(rawId)));
    }

    /// <summary>
    /// Constructor for Spotify Songs.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="spotifyId"></param>
    /// <param name="source"></param>
    /// <exception cref="InvalidSongParameterException"></exception>
    public Song(string title, string artist, TimeSpan duration, string spotifyId, SongSource source = SongSource.Spotify)
    {
        if (string.IsNullOrWhiteSpace(spotifyId))
            throw new InvalidSongParameterException(nameof(spotifyId));

        Title = title.Trim();
        Artist = artist.Trim();
        Duration = duration;
        Source = source;
        Id = spotifyId;
        SourcePath = null;
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