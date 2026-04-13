namespace Core.Commands;

public class Pause : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.Pause();
    }
}
