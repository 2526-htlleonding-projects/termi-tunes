using Core.Dto;

namespace Core.Commands;

public class Play : ICommand
{
    public string Target { get; init; } = string.Empty;
    public bool Shuffle { get; init; }
    public bool Loop { get; set; }

    public async Task ExecuteAsync(PlaybackController controller)
    {
        var song = controller.Search(Target, Shuffle);
        
        await controller.Play(await song, Shuffle);
    }
}
