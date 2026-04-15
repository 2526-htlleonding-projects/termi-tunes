namespace Core.Commands;

public class Skip : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.PlayNext();
    }
}
