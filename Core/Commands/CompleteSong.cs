namespace Core.Commands;

public class CompleteSong : ICommand
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 50;

    public async Task ExecuteAsync(PlaybackController controller)
    {
        var completions = await controller.GetSongCompletions(Query, Limit);
        foreach (var completion in completions)
        {
            Console.WriteLine(completion);
        }
    }
}
