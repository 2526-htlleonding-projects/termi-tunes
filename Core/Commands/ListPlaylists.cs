namespace Core.Commands;

public class ListPlaylists : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        var playlists = await controller.ListPlaylists();
        for (var i = 0; i < playlists.Count; i++)
        {
            Console.WriteLine($"{i}. {playlists[i]}");
        }
    }
}
