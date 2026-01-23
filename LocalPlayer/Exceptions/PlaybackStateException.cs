namespace LocalPlayer.Exceptions;

public class PlaybackStateException : LocalPlayerException
{
    public PlaybackStateException(string action, string state) : 
        base($"Cannot {action} while player is {state}.") {}
}