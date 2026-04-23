namespace Core.Commands;

public class SpotifyAuth : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.SpotifyAuth();
    }
}
