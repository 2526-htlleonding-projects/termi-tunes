namespace CLI.Exceptions;

public class InvalidCommandExceptions : CliException
{
    public InvalidCommandExceptions(string command) : 
        base($"Command {command} is invalid.") {}
}