namespace Core;

/// <summary>
/// The IMusicPlayer interface ties together the Spotify and Local Players. 
/// </summary>
public interface IMusicBackend
{
    Task PlayAsync(Song song);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
}
