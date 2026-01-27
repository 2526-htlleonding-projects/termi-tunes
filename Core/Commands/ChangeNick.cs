namespace Core.Commands;

public class ChangeNick : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        throw new NotImplementedException();
    }

    public string Target { get; set; }
    public string NewNick { get; set; }
}