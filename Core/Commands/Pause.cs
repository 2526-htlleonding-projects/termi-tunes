namespace Core.Commands;

public class Pause : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        controller.Pause();
        return Task.CompletedTask;
    }
}