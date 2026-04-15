namespace Core.Commands;

public class QueueCommand : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.Queue(Track);
    }

    public string Track { get; set; } = string.Empty;
}
