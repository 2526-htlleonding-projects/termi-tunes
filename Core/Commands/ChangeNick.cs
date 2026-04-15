namespace Core.Commands;

public class ChangeNick : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.ChangeNick(Target, NewNick);
    }

    public string Target { get; set; } = string.Empty;
    public string NewNick { get; set; } = string.Empty;
}
