namespace Core.Commands;

public class Search : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        var results = await controller.SearchCatalog(Query);
        if (results.Count == 0)
        {
            Console.WriteLine($"No results for '{Query}'.");
            return;
        }

        for (var i = 0; i < results.Count; i++)
        {
            var song = results[i];
            Console.WriteLine($"{i}. {song.Title} - {song.Artist} [{song.Nickname}]");
        }

        if (AutoPlayFirst)
        {
            await controller.Play(results[0], false);
            Console.WriteLine($"Playing first result: {results[0].Title} - {results[0].Artist}");
            return;
        }

        Console.Write("Select result index to play (empty to cancel): ");
        var input = Console.ReadLine();
        if (int.TryParse(input, out var selected) && selected >= 0 && selected < results.Count)
        {
            await controller.Play(results[selected], false);
            Console.WriteLine($"Playing: {results[selected].Title} - {results[selected].Artist}");
        }
    }

    public string Query { get; set; } = string.Empty;
    public bool AutoPlayFirst { get; set; }
}
