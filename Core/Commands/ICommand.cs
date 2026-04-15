namespace Core.Commands;

public interface ICommand
{
    public Task ExecuteAsync(PlaybackController controller);
}