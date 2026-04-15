namespace Core.Commands;

public class Add : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.Add(Target, Playlist);
    }

    public string? Target { get; set; }
    public string? Playlist { get; set; }
}
