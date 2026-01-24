using Core.Dto;

namespace Core.Commands;

public class Play : ICommand
{
    private readonly IMusicBackend _music;
    private readonly Song _song;

    public Play(IMusicBackend music, Song song)
    {
        _music = music;
        _song = song;
    }

    public Task ExecuteAsync()
    {
        return _music.PlayAsync(_song);
    }
}