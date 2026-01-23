namespace Core;

public static class SongFactory
{
    public static Song CreateLocal(string title, string artist, TimeSpan duration, string path)
    {
        return new Song(title, artist, duration, path);
    }

    public static Song CreateSpotify(string title, string artist, TimeSpan duration, string spotifyId)
    {
        return new Song(title, artist, duration, spotifyId, SongSource.Spotify);
    }
}
