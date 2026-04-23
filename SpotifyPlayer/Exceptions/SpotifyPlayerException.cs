namespace SpotifyPlayer.Exceptions;

public abstract class SpotifyPlayerException : Exception
{
    protected SpotifyPlayerException(string message, Exception? inner = null)
        : base(message, inner) { }
}
