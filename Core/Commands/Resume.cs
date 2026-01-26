namespace Core.Commands;

public class Resume : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        controller.Resume();
        return Task.CompletedTask;
    }
}