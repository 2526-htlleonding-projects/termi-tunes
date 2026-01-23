using Core.Exceptions;

namespace Core;

/// <summary>
/// The Music Master is the main part of this application, it controls playback and stores Playlists via commands that will be sent by the CLI / TLI.
/// </summary>
public class PlaybackController
{
    private const SongSource SPOTIFY = SongSource.Spotify;
    
    private const PlaybackState STOPPED = PlaybackState.Stopped;

    private readonly IMusicBackend _local;
    private readonly IMusicBackend _spotify;
    
    private Queue<Song> _playbackQueue = new();
    private Stack<Song> _playedQueue = new();
    private Song? _currentSong;
    
    private PlaybackState _state = PlaybackState.Stopped;

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
    public Task Play(Song song)
    {
        if (_currentSong != null && !_currentSong.Equals(song)) _playedQueue.Push(_currentSong);
        _currentSong = song;
        _state = PlaybackState.Playing;
        return _currentSong.Source == SPOTIFY ? _spotify.PlayAsync(song) : _local.PlayAsync(song);
    }
    /// <summary>
    /// Pause the playback.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Pause()
    {
        if(_state == STOPPED) throw new NoActiveSongException();
        _state = PlaybackState.Paused;
        return _currentSong is { Source: SPOTIFY } ? _spotify.PauseAsync() : _local.PauseAsync();
    }
    
    /// <summary>
    /// Resume the playback.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Resume()
    {
        if(_state == STOPPED) throw new NoActiveSongException();
        _state = PlaybackState.Playing;
        return _currentSong is { Source: SPOTIFY } ? _spotify.ResumeAsync() : _local.ResumeAsync();
    }

    /// <summary>
    /// Pause playback, clear song, resume no longer possible.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Stop()
    {
        if (_state == STOPPED) throw new NoActiveSongException();

        var backend = _currentSong is { Source: SPOTIFY } ? _spotify : _local;

        if (_currentSong != null) _playedQueue.Push(_currentSong);
        _currentSong = null;

        _state = PlaybackState.Stopped;
        return backend.StopAsync();
    }

    /// <summary>
    /// Play the next song in queue.
    /// </summary>
    /// <returns></returns>
    public Task PlayNext()
    {
        return _playbackQueue.Count == 0 ? 
            throw new InvalidPlaybackStateException("play next", "queue is empty") 
            : Play(_playbackQueue.Dequeue());
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
            : Play(_playedQueue.Pop());
    }
}

// -- Utils --

public enum PlaybackState
{
    Stopped,
    Paused,
    Playing
}