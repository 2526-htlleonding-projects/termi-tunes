namespace Core.Commands;

public class ListSongs : ICommand
{
    public string? Playlist { get; set; }

    public async Task ExecuteAsync(PlaybackController controller)
    {
        var songs = await controller.ListSongs(Playlist);
        if (songs.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(Playlist))
            {
                Console.WriteLine("No songs found in the current playlist.");
            }
            else
            {
                Console.WriteLine($"No songs found in playlist '{Playlist}'.");
            }

            return;
        }

        for (var i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            Console.WriteLine($"{i}. {song.Title} - {song.Artist} [{song.Nickname}]");
        }
    }
}
