namespace Core.Exceptions;

public class InvalidSongParameterException : PlaybackException
{
    public InvalidSongParameterException(string parameter) 
        : base($"The parameter {parameter} is invalid.") {}
}