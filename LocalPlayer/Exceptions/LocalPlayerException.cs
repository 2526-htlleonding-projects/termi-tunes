namespace LocalPlayer.Exceptions;

public abstract class LocalPlayerException : Exception
{
    protected LocalPlayerException(string message, Exception? inner = null)
        : base(message, inner) {}
}