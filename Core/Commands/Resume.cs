namespace Core.Commands;

public class Resume : ICommand
{
    private readonly IMusicBackend _music;

    public Resume(IMusicBackend music)
    {
        _music = music;
    }
    
    public Task ExecuteAsync()
    {
        return  _music.ResumeAsync();
    }
}