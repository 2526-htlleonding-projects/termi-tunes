namespace Core.Commands;

public interface ICommand
{
    Task ExecuteAsync();
}