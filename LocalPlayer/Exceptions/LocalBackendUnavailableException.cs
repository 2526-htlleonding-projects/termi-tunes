namespace LocalPlayer.Exceptions;

public sealed class LocalBackendUnavailableException : LocalPlayerException
{
    public LocalBackendUnavailableException(string message, Exception? inner = null)
        : base(message, inner) {}
}
