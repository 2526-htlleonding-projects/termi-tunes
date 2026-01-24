using Core;
using Core.Dto;

namespace SpotifyPlayer;

/// <summary>
/// This is the link to Spotify's api.
/// </summary>
public class SpotifyPlayer : IMusicBackend
{
    public Task PlayAsync(Song song)
    {
        throw new NotImplementedException();
    }

    public Task PauseAsync()
    {
        throw new NotImplementedException();
    }

    public Task ResumeAsync()
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }
}