namespace CLI.Exceptions;

public class InvalidParameterException : CliException
{
    public InvalidParameterException(string parameter) : 
        base($"Invalid parameter: {parameter}") {}
}