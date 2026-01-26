using Core.Dto;

namespace Core.Commands;

public class Search : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        //TODO make it right
        controller.Search("smth");
        return Task.CompletedTask;
    }

    public string Query { get; set; }
    public bool AutoPlayFirst { get; set; }
}