namespace Core;

public static class SongFactory
{
    public static Song CreateLocal(string title, string artist, TimeSpan duration)
    {
        return new Song(title, artist, duration, SongSource.Local);
    }

    public static Song CreateSpotify(string title, string artist, TimeSpan duration, string spotifyId)
    {
        return new Song(title, artist, duration, SongSource.Spotify, spotifyId);
    }
}
