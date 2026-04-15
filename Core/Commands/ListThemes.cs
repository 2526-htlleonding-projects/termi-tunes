namespace Core.Commands;

public class ListThemes : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        var themes = await controller.ListThemes();
        for (var i = 0; i < themes.Count; i++)
        {
            Console.WriteLine($"{i}. {themes[i]}");
        }
    }
}
