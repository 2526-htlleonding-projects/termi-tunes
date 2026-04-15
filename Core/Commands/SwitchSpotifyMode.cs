namespace Core.Commands;

public class SwitchSpotifyMode : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.SwitchSpotifyMode();
    }
}
