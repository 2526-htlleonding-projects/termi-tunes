namespace Core.Exceptions;

public sealed class InvalidPlaybackStateException : PlaybackException
{
    public InvalidPlaybackStateException(string action, string state)
        : base($"Cannot {action} while player is {state}.") {}
}