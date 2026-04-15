namespace Core.Commands;

public class ListDevices : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        var devices = await controller.ListDevices();
        for (var i = 0; i < devices.Count; i++)
        {
            Console.WriteLine($"{i}. {devices[i]}");
        }
    }
}
