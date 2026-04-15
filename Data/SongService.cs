using System.Data.SQLite;
using Core;
using Core.Dto;
using Core.Exceptions;
using Dapper;
using Data.Exceptions;

namespace Data;

public class SongService : IPlaybackStore
{
    private readonly string _connectionString = "Data Source=music.db";

    private sealed class SongRow
    {
        public string Id { get; set; } = string.Empty;
        public long Source { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public long DurationSeconds { get; set; }
        public string? SourcePath { get; set; }
        public string Nickname { get; set; } = string.Empty;
    }

    private static Song ToSong(SongRow row)
    {
        return new Song(
            row.Id,
            row.Title,
            row.Artist,
            TimeSpan.FromSeconds(row.DurationSeconds),
            (SongSource)checked((int)row.Source),
            row.SourcePath,
            row.Nickname,
            []);
    }

    public Task InitializeAsync() => InitializeInternalAsync();

    private async Task InitializeInternalAsync()
    {
        await using var db = new SQLiteConnection(_connectionString);

        await db.ExecuteAsync(@"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS songs (
                id TEXT NOT NULL,
                source INTEGER NOT NULL,
                title TEXT NOT NULL,
                artist TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                source_path TEXT NULL,
                nickname TEXT NOT NULL,
                PRIMARY KEY (id, source)
            );

            CREATE TABLE IF NOT EXISTS nicknames (
                nickname TEXT PRIMARY KEY,
                song_id TEXT NOT NULL,
                source INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS playlists (
                name TEXT PRIMARY KEY
            );

            CREATE TABLE IF NOT EXISTS playlist_songs (
                playlist_name TEXT NOT NULL,
                song_id TEXT NOT NULL,
                source INTEGER NOT NULL,
                added_at INTEGER NOT NULL DEFAULT (strftime('%s','now')),
                PRIMARY KEY (playlist_name, song_id, source)
            );

            CREATE TABLE IF NOT EXISTS queue (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                song_id TEXT NOT NULL,
                source INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                song_id TEXT NOT NULL,
                source INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS devices (
                name TEXT PRIMARY KEY
            );
        ");

        await db.ExecuteAsync("INSERT OR IGNORE INTO playlists (name) VALUES ('Recents');");
        await db.ExecuteAsync("INSERT OR IGNORE INTO settings (key, value) VALUES ('theme', 'default');");
        await db.ExecuteAsync("INSERT OR IGNORE INTO settings (key, value) VALUES ('mode', 'local');");
        await db.ExecuteAsync("INSERT OR IGNORE INTO devices (name) VALUES ('computer');");
        await db.ExecuteAsync("INSERT OR IGNORE INTO devices (name) VALUES ('phone');");
        await db.ExecuteAsync("INSERT OR IGNORE INTO devices (name) VALUES ('tv');");
        await db.ExecuteAsync("INSERT OR IGNORE INTO settings (key, value) VALUES ('device', 'computer');");
    }

    public async Task UpsertSongAsync(Song song)
    {
        await using var db = new SQLiteConnection(_connectionString);

        await db.ExecuteAsync(
            @"INSERT INTO songs (id, source, title, artist, duration_seconds, source_path, nickname)
              VALUES (@Id, @Source, @Title, @Artist, @DurationSeconds, @SourcePath, @Nickname)
              ON CONFLICT(id, source) DO UPDATE SET
                  title = excluded.title,
                  artist = excluded.artist,
                  duration_seconds = excluded.duration_seconds,
                  source_path = excluded.source_path,
                  nickname = excluded.nickname;",
            new
            {
                song.Id,
                Source = (int)song.Source,
                song.Title,
                song.Artist,
                DurationSeconds = (long)song.Duration.TotalSeconds,
                SourcePath = string.IsNullOrWhiteSpace(song.SourcePath) ? null : song.SourcePath,
                song.Nickname
            });

        var existing = await db.QueryFirstOrDefaultAsync<(string SongId, int Source)>(
            @"SELECT song_id AS SongId, source AS Source
              FROM nicknames
              WHERE nickname = @nickname;",
            new { nickname = song.Nickname });

        if (string.IsNullOrWhiteSpace(existing.SongId))
        {
            await db.ExecuteAsync(
                "INSERT INTO nicknames (nickname, song_id, source) VALUES (@Nickname, @SongId, @Source);",
                new { Nickname = song.Nickname, SongId = song.Id, Source = (int)song.Source });
        }
    }

    public async Task<Song?> GetSongAsync(string songId, SongSource source)
    {
        await using var db = new SQLiteConnection(_connectionString);
        var row = await db.QueryFirstOrDefaultAsync<SongRow>(
            @"SELECT id, source, title, artist, duration_seconds AS DurationSeconds, source_path AS SourcePath, nickname
              FROM songs
              WHERE id = @songId AND source = @source;",
            new { songId, source = (int)source });

        return row == null ? null : ToSong(row);
    }

    public async Task<Song?> GetSongByNicknameAsync(string nickname)
    {
        await using var db = new SQLiteConnection(_connectionString);
        var row = await db.QueryFirstOrDefaultAsync<SongRow>(
            @"SELECT s.id, s.source, s.title, s.artist, s.duration_seconds AS DurationSeconds, s.source_path AS SourcePath, n.nickname
              FROM nicknames n
              JOIN songs s ON s.id = n.song_id AND s.source = n.source
              WHERE n.nickname = @nickname;",
            new { nickname });

        return row == null ? null : ToSong(row);
    }

    public async Task<IReadOnlyList<Song>> SearchSongsAsync(string query, int limit = 10)
    {
        await using var db = new SQLiteConnection(_connectionString);
        var rows = await db.QueryAsync<SongRow>(
            @"SELECT id, source, title, artist, duration_seconds AS DurationSeconds, source_path AS SourcePath, nickname
              FROM songs
              WHERE title LIKE @q OR artist LIKE @q OR nickname LIKE @q
              ORDER BY title
              LIMIT @limit;",
            new { q = $"%{query}%", limit });

        return rows.Select(ToSong).ToList();
    }

    public async Task SetNicknameAsync(string songId, SongSource source, string nickname)
    {
        var normalized = SongFactory.GenerateDefaultNickname(nickname);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidSongParameterException("nickname");
        }

        await using var db = new SQLiteConnection(_connectionString);

        var existing = await db.QueryFirstOrDefaultAsync<(string SongId, int Source)>(
            @"SELECT song_id AS SongId, source AS Source
              FROM nicknames
              WHERE nickname = @nickname;",
            new { nickname = normalized });

        if (!string.IsNullOrWhiteSpace(existing.SongId) &&
            !(string.Equals(existing.SongId, songId, StringComparison.Ordinal) && existing.Source == (int)source))
        {
            throw new NicknameAlreadyExistsException(normalized);
        }

        await db.ExecuteAsync(
            @"DELETE FROM nicknames WHERE song_id = @songId AND source = @source;
              INSERT INTO nicknames (nickname, song_id, source) VALUES (@nickname, @songId, @source);
              UPDATE songs SET nickname = @nickname WHERE id = @songId AND source = @source;",
            new { nickname = normalized, songId, source = (int)source });
    }

    public async Task<bool> NicknameExistsAsync(string nickname)
    {
        await using var db = new SQLiteConnection(_connectionString);
        var count = await db.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM nicknames WHERE nickname = @nickname;",
            new { nickname });
        return count > 0;
    }

    public async Task AddSongToPlaylistAsync(string playlist, Song song)
    {
        await EnsurePlaylistAsync(playlist);
        await UpsertSongAsync(song);

        await using var db = new SQLiteConnection(_connectionString);
        await db.ExecuteAsync(
            @"INSERT OR IGNORE INTO playlist_songs (playlist_name, song_id, source)
              VALUES (@playlist, @songId, @source);",
            new { playlist, songId = song.Id, source = (int)song.Source });
    }

    public async Task<IReadOnlyList<string>> GetPlaylistsAsync()
    {
        await using var db = new SQLiteConnection(_connectionString);
        var rows = await db.QueryAsync<string>(
            "SELECT name FROM playlists ORDER BY name;");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Song>> GetPlaylistSongsAsync(string playlist)
    {
        await using var db = new SQLiteConnection(_connectionString);
        var rows = await db.QueryAsync<SongRow>(
            @"SELECT s.id, s.source, s.title, s.artist, s.duration_seconds AS DurationSeconds, s.source_path AS SourcePath, s.nickname
              FROM playlist_songs ps
              JOIN songs s ON s.id = ps.song_id AND s.source = ps.source
              WHERE ps.playlist_name = @playlist
              ORDER BY ps.added_at;",
            new { playlist });

        return rows.Select(ToSong).ToList();
    }

    public async Task EnsurePlaylistAsync(string playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist))
        {
            throw new InvalidSongParameterException("playlist");
        }

        await using var db = new SQLiteConnection(_connectionString);
        await db.ExecuteAsync(
            "INSERT OR IGNORE INTO playlists (name) VALUES (@playlist);",
            new { playlist });
    }

    public async Task SetCurrentPlaylistAsync(string playlist)
    {
        await EnsurePlaylistAsync(playlist);
        await SetSettingAsync("current_playlist", playlist);
    }

    public async Task<string?> GetCurrentPlaylistAsync()
    {
        return await GetSettingAsync("current_playlist");
    }

    public async Task SetCurrentSongAsync(Song? song)
    {
        if (song == null)
        {
            await SetSettingAsync("current_song_id", string.Empty);
            await SetSettingAsync("current_song_source", string.Empty);
            return;
        }

        await UpsertSongAsync(song);
        await SetSettingAsync("current_song_id", song.Id);
        await SetSettingAsync("current_song_source", ((int)song.Source).ToString());
    }

    public async Task<Song?> GetCurrentSongAsync()
    {
        var songId = await GetSettingAsync("current_song_id");
        var sourceRaw = await GetSettingAsync("current_song_source");

        if (string.IsNullOrWhiteSpace(songId) || string.IsNullOrWhiteSpace(sourceRaw))
        {
            return null;
        }

        if (!int.TryParse(sourceRaw, out var sourceInt))
        {
            return null;
        }

        return await GetSongAsync(songId, (SongSource)sourceInt);
    }

    public async Task EnqueueAsync(Song song)
    {
        await UpsertSongAsync(song);
        await using var db = new SQLiteConnection(_connectionString);
        await db.ExecuteAsync(
            "INSERT INTO queue (song_id, source) VALUES (@songId, @source);",
            new { songId = song.Id, source = (int)song.Source });
    }

    public async Task<Song?> DequeueAsync()
    {
        await using var db = new SQLiteConnection(_connectionString);
        var item = await db.QueryFirstOrDefaultAsync<(long Id, string SongId, int Source)>(
            "SELECT id, song_id AS SongId, source AS Source FROM queue ORDER BY id LIMIT 1;");

        if (item.Id == 0)
        {
            return null;
        }

        await db.ExecuteAsync("DELETE FROM queue WHERE id = @id;", new { id = item.Id });
        return await GetSongAsync(item.SongId, (SongSource)item.Source);
    }

    public async Task PushHistoryAsync(Song song)
    {
        await UpsertSongAsync(song);
        await using var db = new SQLiteConnection(_connectionString);
        await db.ExecuteAsync(
            "INSERT INTO history (song_id, source) VALUES (@songId, @source);",
            new { songId = song.Id, source = (int)song.Source });
    }

    public async Task<Song?> PopHistoryAsync()
    {
        await using var db = new SQLiteConnection(_connectionString);
        var item = await db.QueryFirstOrDefaultAsync<(long Id, string SongId, int Source)>(
            "SELECT id, song_id AS SongId, source AS Source FROM history ORDER BY id DESC LIMIT 1;");

        if (item.Id == 0)
        {
            return null;
        }

        await db.ExecuteAsync("DELETE FROM history WHERE id = @id;", new { id = item.Id });
        return await GetSongAsync(item.SongId, (SongSource)item.Source);
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await using var db = new SQLiteConnection(_connectionString);
        await db.ExecuteAsync(
            @"INSERT INTO settings (key, value) VALUES (@key, @value)
              ON CONFLICT(key) DO UPDATE SET value = excluded.value;",
            new { key, value });
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        await using var db = new SQLiteConnection(_connectionString);
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT value FROM settings WHERE key = @key;",
            new { key });
    }

    public async Task<IReadOnlyList<string>> GetDevicesAsync()
    {
        await using var db = new SQLiteConnection(_connectionString);
        var rows = await db.QueryAsync<string>("SELECT name FROM devices ORDER BY name;");
        return rows.ToList();
    }

    public async Task AddDeviceIfMissingAsync(string deviceName)
    {
        await using var db = new SQLiteConnection(_connectionString);
        await db.ExecuteAsync(
            "INSERT OR IGNORE INTO devices (name) VALUES (@deviceName);",
            new { deviceName });
    }
}
