namespace Core.Commands;

public class Pause : ICommand
{
    private readonly IMusicBackend _music;

    public Pause(IMusicBackend music)
    {
        _music = music;
    }
    
    public Task ExecuteAsync()
    {
        return _music.PauseAsync();
    }
}