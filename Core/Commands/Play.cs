namespace Core.Commands;

public class Play : ICommand
{
    private readonly PlaybackController _music;
    private readonly Song _song;

    public Play(PlaybackController music, Song song)
    {
        _music = music;
        _song = song;
    }

    public Task ExecuteAsync()
    {
        return _music.PlayAsync(_song);
    }
}