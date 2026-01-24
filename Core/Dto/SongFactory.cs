namespace Core.Dto;

/// <summary>
/// Here is the creation and management of Songs.
/// </summary>
public static class SongFactory
{
    /// <summary>
    /// Create a Dummy Song
    /// </summary>
    /// <returns></returns>
    public static Song CreateDummy()
    {
        return new Song("id", "title", "me", TimeSpan.Zero, SongSource.Local, null, "dummy", null);
    }
    
    /// <summary>
    /// Returns a new Local Song.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="path"></param>
    /// <param name="creators"></param>
    /// <returns></returns>
    public static Song CreateLocal(string title, string artist, TimeSpan duration, string path, List<Artist> creators)
    {
        var cleanTitle = title.Trim();
        var cleanArtist = artist.Trim();
        
        var rawId = $"{cleanArtist}|{cleanTitle}|{duration.TotalSeconds}";
        var id = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(rawId)));

        var nickname = GenerateDefaultNickname(cleanTitle);
        
        return new Song(id, cleanTitle, cleanArtist, duration, SongSource.Local, path, nickname, creators);
    }

    /// <summary>
    /// Returns a new Spotify Song.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="artist"></param>
    /// <param name="duration"></param>
    /// <param name="spotifyId"></param
    /// <param name="creators"></param>
    /// <returns></returns>
    public static Song CreateSpotify(string title, string artist, TimeSpan duration, string spotifyId, List<Artist> creators)
    {
        var cleanTitle = title.Trim();
        var nickname = GenerateDefaultNickname(cleanTitle);

        return new Song(spotifyId, cleanTitle, artist.Trim(), duration, SongSource.Spotify, null, nickname, creators);
    }

    /// <summary>
    /// Generates a unique ID for a song.
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    private static string GenerateDefaultNickname(string title)
    {
        return System.Text.RegularExpressions.Regex.Replace(title.ToLower(), @"[^a-z0-9]+", "_").Trim('_');
    }
}