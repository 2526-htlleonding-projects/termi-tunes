namespace Data.Exceptions;

public class DatabaseNotInitializedException : DatabaseException
{
    public DatabaseNotInitializedException() : 
        base("Database could not be initialized.") {}
}