namespace Data.Exceptions;

public class DatabaseException : Exception
{
    protected DatabaseException(string message, Exception? inner = null)
        : base(message, inner) {}    
}