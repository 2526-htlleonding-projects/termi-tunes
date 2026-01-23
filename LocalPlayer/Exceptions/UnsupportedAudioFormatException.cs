namespace LocalPlayer.Exceptions;

public class UnsupportedAudioFormatException : LocalPlayerException
{
    public UnsupportedAudioFormatException(string file) : 
        base($"Unsupported audio format: {file}") {}
}