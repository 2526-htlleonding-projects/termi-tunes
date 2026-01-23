namespace CLI.Exceptions;

public class MissingCommandException : CliException
{
    public MissingCommandException() : 
        base("No parameters provided.") {}
}