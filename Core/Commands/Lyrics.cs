namespace Core.Commands;

public class Lyrics : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.Lyrics(PrintAll);
    }

    public bool PrintAll { get; set; }
}
