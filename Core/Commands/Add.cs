namespace Core.Commands;

public class Add : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        throw new NotImplementedException();
    }

    public string Playlist { get; set; }
}