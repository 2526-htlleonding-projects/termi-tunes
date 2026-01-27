namespace Core.Commands;

public class QueueCommand : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        throw new NotImplementedException();
    }

    public string Track { get; set; }
}