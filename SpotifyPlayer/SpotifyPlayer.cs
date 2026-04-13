using Core;
using Core.Dto;

namespace SpotifyPlayer;

/// <summary>
/// Placeholder Spotify backend.
/// This keeps the CLI functional until real Spotify API integration is implemented.
/// </summary>
public class SpotifyPlayer : IMusicBackend, ILyricsProvider
{
    public Task PlayAsync(Song song)
    {
        Console.WriteLine($"Spotify play: {song.Title} - {song.Artist}");
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        Console.WriteLine("Spotify pause.");
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        Console.WriteLine("Spotify resume.");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Console.WriteLine("Spotify stop.");
        return Task.CompletedTask;
    }

    public Task GetLyrics(Song song)
    {
        Console.WriteLine($"Lyrics for '{song.Title}' are not integrated yet.");
        return Task.CompletedTask;
    }
}
