namespace Core.Commands;

public class SwitchLocalMode : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.SwitchLocalMode();
    }
}
