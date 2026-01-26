namespace Core.Commands;

public class Stop : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        controller.Stop();
        return Task.CompletedTask;
    }
}