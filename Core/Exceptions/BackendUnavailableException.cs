namespace Core.Exceptions;

public sealed class BackendUnavailableException : PlaybackException
{
    public BackendUnavailableException(string backend, Exception? inner = null)
        : base($"{backend} backend is unavailable.", inner) {}
}