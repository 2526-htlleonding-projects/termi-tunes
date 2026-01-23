namespace Core.Exceptions;

public abstract class PlaybackException : Exception
{
    protected PlaybackException(string message, Exception? inner = null)
        : base(message, inner) {}
}
