using Core.Dto;

namespace Core;

public interface IPlaybackStore
{
    Task InitializeAsync();

    Task UpsertSongAsync(Song song);
    Task<Song?> GetSongAsync(string songId, SongSource source);
    Task<Song?> GetSongByNicknameAsync(string nickname);
    Task<IReadOnlyList<Song>> SearchSongsAsync(string query, int limit = 10);

    Task SetNicknameAsync(string songId, SongSource source, string nickname);
    Task<bool> NicknameExistsAsync(string nickname);

    Task AddSongToPlaylistAsync(string playlist, Song song);
    Task<IReadOnlyList<string>> GetPlaylistsAsync();
    Task<IReadOnlyList<Song>> GetPlaylistSongsAsync(string playlist);
    Task EnsurePlaylistAsync(string playlist);

    Task SetCurrentPlaylistAsync(string playlist);
    Task<string?> GetCurrentPlaylistAsync();

    Task SetCurrentSongAsync(Song? song);
    Task<Song?> GetCurrentSongAsync();

    Task EnqueueAsync(Song song);
    Task<Song?> DequeueAsync();
    Task PushHistoryAsync(Song song);
    Task<Song?> PopHistoryAsync();

    Task SetSettingAsync(string key, string value);
    Task<string?> GetSettingAsync(string key);

    Task<IReadOnlyList<string>> GetDevicesAsync();
    Task AddDeviceIfMissingAsync(string deviceName);
}
