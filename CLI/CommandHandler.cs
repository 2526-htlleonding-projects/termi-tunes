using Core;

namespace CLI;

public class CommandHandler
{
    private PlaybackController _playbackController;

    public CommandHandler(PlaybackController playbackController)
    {
        _playbackController = playbackController;
    }
    
    
}