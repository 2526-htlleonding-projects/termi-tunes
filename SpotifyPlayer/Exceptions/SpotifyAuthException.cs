namespace SpotifyPlayer.Exceptions;

public sealed class SpotifyAuthException : SpotifyPlayerException
{
    public SpotifyAuthException(string message, Exception? inner = null)
        : base(message, inner) { }
}
