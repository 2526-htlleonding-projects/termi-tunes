namespace Core.Commands;

public class Stop : ICommand
{
    private readonly IMusicBackend _music;
    
    public Stop(IMusicBackend music)
    {
        _music = music;
    }
    
    public Task ExecuteAsync()
    {
        return _music.StopAsync();
    }
}