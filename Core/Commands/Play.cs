using Core.Dto;

namespace Core.Commands;

public class Play : ICommand
{
    public string Target { get; init; } 
    public bool Shuffle { get; init; }
    public bool Loop { get; set; }

    public async Task ExecuteAsync(PlaybackController controller)
    {
        var song = controller.Search(Target);
        
        await controller.Play(song, Shuffle);
    }
}