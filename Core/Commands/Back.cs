namespace Core.Commands;

/// <summary>
/// Play prev. song
/// </summary>
public class Back : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.PlayPrevious();
    }
}