using Core.Dto;

namespace Core;

/// <summary>
/// Here is the creation and management of Songs.
/// </summary>
public static class SongFactory
{
    /// <summary>
    /// Returns a new Local Song.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Song CreateLocal(string title, string artist, TimeSpan duration, string path)
    {
        var cleanTitle = title.Trim();
        var cleanArtist = artist.Trim();
        
        var rawId = $"{cleanArtist}|{cleanTitle}|{duration.TotalSeconds}";
        var id = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(rawId)));

        var nickname = GenerateDefaultNickname(cleanTitle);

        var creators = new List<Artist>();
        
        return new Song(id, cleanTitle, cleanArtist, duration, SongSource.Local, path, nickname, creators);
    }

    /// <summary>
    /// Returns a new Spotify Song.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="spotifyId"></param>
    /// <returns></returns>
    public static Song CreateSpotify(string title, string artist, TimeSpan duration, string spotifyId)
    {
        var cleanTitle = title.Trim();
        var nickname = GenerateDefaultNickname(cleanTitle);
        
        var creators = new List<Artist>();

        return new Song(spotifyId, cleanTitle, artist.Trim(), duration, SongSource.Spotify, null, nickname, creators);
    }

    private static string GenerateDefaultNickname(string title)
    {
        // Simple regex to make it CLI friendly: lowercase and underscores
        return System.Text.RegularExpressions.Regex.Replace(title.ToLower(), @"[^a-z0-9]+", "_").Trim('_');
    }
}