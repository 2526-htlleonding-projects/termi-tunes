namespace Core.Commands;

public class ChangePlaylist : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.ChangePlaylist(Name);
    }

    public string Name { get; set; } = string.Empty;
}
