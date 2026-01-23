namespace CLI.Exceptions;

public abstract class CliException : Exception
{
    protected CliException(string message, Exception? inner = null)
        : base(message, inner) {}
}