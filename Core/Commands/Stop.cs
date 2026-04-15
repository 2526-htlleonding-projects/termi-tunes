namespace Core.Commands;

public class Stop : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.Stop();
    }
}
