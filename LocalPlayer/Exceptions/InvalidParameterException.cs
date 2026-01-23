namespace LocalPlayer.Exceptions;

public class InvalidParameterException : LocalPlayerException
{
    public InvalidParameterException(string parameter) : 
        base($"Invalid parameter: {parameter}") {}
}