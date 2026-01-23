using Core.Exceptions;
using static Core.IMusicBackend;

namespace Core;

/// <summary>
/// The Music Master is the main part of this application, it controls playback and stores Playlists via commands that will be sent by the CLI / TLI.
/// </summary>
public class PlaybackController : IMusicBackend
{
    private const SongSource SPOTIFY = SongSource.Spotify;
    private const SongSource LOCAL = SongSource.Local;
    
    private readonly IMusicBackend _local;
    private readonly IMusicBackend _spotify;
    
    private Queue<Song> _playbackQueue = new();
    private Stack<Song> _playedQueue = new();
    private Song? _currentSong;

    public PlaybackController(IMusicBackend local, IMusicBackend spotify)
    {
        _local = local;
        _spotify = spotify;
    }

    /// <summary>
    /// Play a given song.
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task PlayAsync(Song song)
    {
        if (_currentSong != null) _playedQueue.Push(_currentSong);
        _currentSong = song;
        return _currentSong.Source == SPOTIFY ? _spotify.PlayAsync(song) : _local.PlayAsync(song);
    }
    /// <summary>
    /// Pause the playback.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task PauseAsync()
    {
        if(_currentSong == null) throw new NoActiveSongException();
        return _currentSong is { Source: SPOTIFY } ? _spotify.PauseAsync() : _local.PauseAsync();
    }
    
    /// <summary>
    /// Resume the playback.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task ResumeAsync()
    {
        if(_currentSong == null) throw new NoActiveSongException();
        return _currentSong is { Source: SPOTIFY } ? _spotify.ResumeAsync() : _local.ResumeAsync();
    }

    /// <summary>
    /// Pause playback, clear song, resume no longer possible.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task StopAsync()
    {
        if(_currentSong == null) throw new NoActiveSongException();
        _playedQueue.Push(_currentSong);
        _currentSong = null;
        return _local.StopAsync();
    }

    /// <summary>
    /// Play the next song in queue.
    /// </summary>
    /// <returns></returns>
    public Task PlayNext()
    {
        return _playbackQueue.Count == 0 ? 
            throw new InvalidPlaybackStateException("play next", "queue is empty") 
            : PlayAsync(_playbackQueue.Dequeue());
    }
    
    /// <summary>
    /// Play the last addition to played Songs.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidPlaybackStateException"></exception>
    public Task PlayPrevious()
    {
        return _playedQueue.Count == 0
            ? throw new InvalidPlaybackStateException("play previous", "not been played before")
            : PlayAsync(_playedQueue.Pop());
    }
}
