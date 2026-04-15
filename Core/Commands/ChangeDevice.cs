namespace Core.Commands;

public class ChangeDevice : ICommand
{
    public string DeviceName { get; set; } = string.Empty;

    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.ChangeDevice(DeviceName);
    }
}
