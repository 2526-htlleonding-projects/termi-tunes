namespace Core.Exceptions;

public sealed class NoActiveSongException : PlaybackException
{
    public NoActiveSongException()
        : base("No song is currently active.") {}
}