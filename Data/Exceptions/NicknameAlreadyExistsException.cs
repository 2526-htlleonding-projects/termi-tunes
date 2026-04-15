namespace Data.Exceptions;

public class NicknameAlreadyExistsException : DatabaseException
{
    public NicknameAlreadyExistsException(string nickname) : 
        base($"nickname '{nickname}' already exists.") {}
}