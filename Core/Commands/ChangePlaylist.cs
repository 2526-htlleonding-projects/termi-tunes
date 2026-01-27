namespace Core.Commands;

public class ChangePlaylist : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        throw new NotImplementedException();
    }

    public string Name { get; set; }
}