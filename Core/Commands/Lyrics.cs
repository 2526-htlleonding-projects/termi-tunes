namespace Core.Commands;

public class Lyrics : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        //TODO make it fucking right
        controller.Lyrics();
        return Task.CompletedTask;
    }

    public bool PrintAll { get; set; }
}