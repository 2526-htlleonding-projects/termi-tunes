using Core.Dto;
using Core.Exceptions;
using System.Text.RegularExpressions;

namespace Core;

/// <summary>
/// The Music Master is the main part of this application, it controls playback and stores Playlists via commands that will be sent by the CLI / TLI.
/// </summary>
public class PlaybackController
{
    private const SongSource SPOTIFY = SongSource.Spotify;

    private readonly IMusicBackend _local;
    private readonly IMusicBackend _spotify;
    private readonly IPlaybackStore _store;
    private readonly IMetadataProvider _localMetadata;
    private readonly ILyricsProvider _lyricsProvider;
    private readonly string _libraryRoot;
    private readonly Random _random = new();

    public PlaybackController(
        IMusicBackend local,
        IMusicBackend spotify,
        IPlaybackStore store,
        IMetadataProvider localMetadata,
        ILyricsProvider lyricsProvider,
        string libraryRoot)
    {
        _local = local;
        _spotify = spotify;
        _store = store;
        _localMetadata = localMetadata;
        _lyricsProvider = lyricsProvider;
        _libraryRoot = libraryRoot;
    }

    public Task InitializeAsync() => _store.InitializeAsync();

    private static string SanitizeNickname(string value)
        => Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');

    private static bool LooksLikePlaylistIndex(string target)
        => target.StartsWith('#') && int.TryParse(target[1..], out _);

    private static bool TryParsePlaylistIndex(string selector, out int index)
    {
        if (selector.StartsWith('#'))
        {
            return int.TryParse(selector[1..], out index);
        }

        return int.TryParse(selector, out index);
    }

    private async Task<Song> SelectPlaylistSongAsync(IReadOnlyList<Song> songs, bool shuffle, bool smartShuffle)
    {
        if (songs.Count == 0)
        {
            throw new InvalidPlaybackStateException("play", "playlist is empty");
        }

        if (smartShuffle)
        {
            var current = await _store.GetCurrentSongAsync();
            if (current != null)
            {
                var next = songs.FirstOrDefault(s => !s.Equals(current));
                if (next != null)
                {
                    return next;
                }
            }

            return songs[_random.Next(songs.Count)];
        }

        if (shuffle)
        {
            return songs[_random.Next(songs.Count)];
        }

        return songs[0];
    }

    private async Task<Song> ResolvePlaylistSongAsync(string target, bool shuffle, bool smartShuffle)
    {
        string playlistName;

        if (LooksLikePlaylistIndex(target))
        {
            var playlists = await _store.GetPlaylistsAsync();
            var index = int.Parse(target[1..]);
            if (index < 0 || index >= playlists.Count)
            {
                throw new InvalidSongParameterException($"playlist index {target}");
            }

            playlistName = playlists[index];
        }
        else
        {
            playlistName = target;
        }

        var songs = await _store.GetPlaylistSongsAsync(playlistName);
        if (songs.Count == 0)
        {
            throw new InvalidPlaybackStateException("play playlist", $"playlist '{playlistName}' is empty");
        }

        await _store.SetCurrentPlaylistAsync(playlistName);
        return await SelectPlaylistSongAsync(songs, shuffle, smartShuffle);
    }

    private IEnumerable<string> EnumerateAudioFiles(string root)
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac", ".wma"
        };

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(root, "*", options);
        }
        catch
        {
            yield break;
        }

        using var enumerator = files.GetEnumerator();
        while (true)
        {
            string file;
            try
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }
                file = enumerator.Current;
            }
            catch
            {
                yield break;
            }

            if (extensions.Contains(Path.GetExtension(file)))
            {
                yield return file;
            }
        }
    }

    private async Task<Song> BuildLocalSongAsync(string path)
    {
        Song song;
        try
        {
            song = await _localMetadata.GetMetadataAsync(path);
        }
        catch
        {
            var name = Path.GetFileNameWithoutExtension(path);
            song = SongFactory.CreateLocal(name, "Unknown", TimeSpan.Zero, path, [new Artist("unknown", "Unknown")]);
        }

        await _store.UpsertSongAsync(song);
        return song;
    }

    private async Task<IReadOnlyList<Song>> SearchLibraryAsync(string query, int limit = 10)
    {
        var fromStore = await _store.SearchSongsAsync(query, limit);
        var songs = new List<Song>(fromStore);
        var existing = new HashSet<string>(songs.Select(s => $"{s.Source}:{s.Id}"));

        foreach (var file in EnumerateAudioFiles(_libraryRoot))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!fileName.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var song = await BuildLocalSongAsync(file);
            var key = $"{song.Source}:{song.Id}";
            if (existing.Add(key))
            {
                songs.Add(song);
                if (songs.Count >= limit)
                {
                    break;
                }
            }
        }

        return songs;
    }

    public async Task<IReadOnlyList<Song>> SearchCatalog(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return await SearchLibraryAsync(query.Trim(), limit);
    }

    public async Task<IReadOnlyList<string>> GetSongCompletions(string query, int limit = 50)
    {
        if (limit <= 0)
        {
            return [];
        }

        var normalized = query.Trim();
        var fetchLimit = Math.Max(200, limit * 20);
        var matches = await _store.SearchSongsAsync(normalized, fetchLimit);

        var currentPlaylist = await _store.GetCurrentPlaylistAsync();
        var preferred = string.IsNullOrWhiteSpace(currentPlaylist)
            ? []
            : await _store.GetPlaylistSongsAsync(currentPlaylist);
        var preferredKeys = new HashSet<string>(preferred.Select(s => $"{s.Source}:{s.Id}"));

        var ranked = matches
            .Select(song => new
            {
                Song = song,
                InCurrentPlaylist = preferredKeys.Contains($"{song.Source}:{song.Id}"),
                NicknameStartsWith = string.IsNullOrWhiteSpace(normalized) ||
                                     song.Nickname.StartsWith(normalized, StringComparison.OrdinalIgnoreCase),
                TitleStartsWith = string.IsNullOrWhiteSpace(normalized) ||
                                  song.Title.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)
            })
            .OrderByDescending(x => x.InCurrentPlaylist)
            .ThenByDescending(x => x.NicknameStartsWith)
            .ThenByDescending(x => x.TitleStartsWith)
            .ThenBy(x => x.Song.Nickname, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Song.Nickname)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();

        return ranked;
    }

    private async Task<Song> ResolveTargetAsync(string target, bool shuffle, bool smartShuffle)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            var playlist = await _store.GetCurrentPlaylistAsync();
            if (string.IsNullOrWhiteSpace(playlist))
            {
                throw new InvalidSongParameterException("target");
            }

            var songs = await _store.GetPlaylistSongsAsync(playlist);
            if (songs.Count == 0)
            {
                throw new InvalidPlaybackStateException("play", $"playlist '{playlist}' is empty");
            }

            return await SelectPlaylistSongAsync(songs, shuffle, smartShuffle);
        }

        target = target.Trim();

        if (LooksLikePlaylistIndex(target))
        {
            return await ResolvePlaylistSongAsync(target, shuffle, smartShuffle);
        }

        var byNick = await _store.GetSongByNicknameAsync(target);
        if (byNick != null)
        {
            return byNick;
        }

        var playlistCandidates = await _store.GetPlaylistSongsAsync(target);
        if (playlistCandidates.Count > 0)
        {
            await _store.SetCurrentPlaylistAsync(target);
            return await SelectPlaylistSongAsync(playlistCandidates, shuffle, smartShuffle);
        }

        if (File.Exists(target))
        {
            return await BuildLocalSongAsync(target);
        }

        var results = await SearchLibraryAsync(target, 1);
        if (results.Count == 0)
        {
            throw new InvalidSongParameterException($"song or playlist '{target}'");
        }

        return results[0];
    }

    private async Task EnsureUniqueNicknameAsync(Song song)
    {
        var baseNick = SongFactory.GenerateDefaultNickname(song.Title);
        var candidate = baseNick;
        var suffix = SanitizeNickname(song.Artist);
        var counter = 2;

        while (await _store.NicknameExistsAsync(candidate))
        {
            candidate = string.IsNullOrWhiteSpace(suffix)
                ? $"{baseNick}_{counter++}"
                : $"{baseNick}_{suffix}";

            if (candidate == $"{baseNick}_{suffix}")
            {
                suffix = $"{suffix}_{counter++}";
            }
        }

        if (!string.Equals(song.Nickname, candidate, StringComparison.Ordinal))
        {
            await _store.SetNicknameAsync(song.Id, song.Source, candidate);
        }
    }

    private IMusicBackend BackendFor(Song song) => song.Source == SPOTIFY ? _spotify : _local;

    private async Task<bool> IsSpotifyOnlyModeAsync()
        => string.Equals(await _store.GetSettingAsync("mode"), "spotify", StringComparison.OrdinalIgnoreCase);

    private async Task EnsureSongModeCompatibilityAsync(Song song)
    {
        if (song.Source != SongSource.Spotify && await IsSpotifyOnlyModeAsync())
        {
            throw new BackendUnavailableException("local");
        }
    }

    private async Task<Song> GetCurrentSongOrThrow()
    {
        var current = await _store.GetCurrentSongAsync();
        if (current == null)
        {
            throw new NoActiveSongException();
        }

        return current;
    }

    /// <summary>
    /// Play a given song.
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Play(Song song, bool shuffle, bool loop = false, bool smartShuffle = false)
    {
        return PlayInternal(song, shuffle, loop, smartShuffle);
    }

    private async Task PlayInternal(Song song, bool shuffle, bool loop, bool smartShuffle)
    {
        await EnsureSongModeCompatibilityAsync(song);

        var previous = await _store.GetCurrentSongAsync();
        if (previous != null && !previous.Equals(song))
        {
            await _store.PushHistoryAsync(previous);
        }

        await _store.UpsertSongAsync(song);
        await EnsureUniqueNicknameAsync(song);
        await _store.SetCurrentSongAsync(song);
        await _store.SetSettingAsync("shuffle", shuffle ? "true" : "false");
        await _store.SetSettingAsync("shuffle_mode", smartShuffle ? "smart" : shuffle ? "shuffle" : "off");
        await _store.SetSettingAsync("loop", loop ? "true" : "false");
        await _store.SetSettingAsync("playback_state", PlaybackState.Playing.ToString());
        await _store.AddSongToPlaylistAsync("Recents", song);

        await BackendFor(song).PlayAsync(song);
    }
    /// <summary>
    /// Pause the playback.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Pause()
    {
        return PauseInternal();
    }

    private async Task PauseInternal()
    {
        var current = await GetCurrentSongOrThrow();
        await BackendFor(current).PauseAsync();
        await _store.SetSettingAsync("playback_state", PlaybackState.Paused.ToString());
    }
    
    /// <summary>
    /// Resume the playback.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Resume()
    {
        return ResumeInternal();
    }

    private async Task ResumeInternal()
    {
        var current = await GetCurrentSongOrThrow();
        await BackendFor(current).ResumeAsync();
        await _store.SetSettingAsync("playback_state", PlaybackState.Playing.ToString());
    }

    /// <summary>
    /// Pause playback, clear song, resume no longer possible.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task Stop()
    {
        return StopInternal();
    }

    private async Task StopInternal()
    {
        var currentSong = await GetCurrentSongOrThrow();
        await BackendFor(currentSong).StopAsync();
        await _store.PushHistoryAsync(currentSong);
        await _store.SetCurrentSongAsync(null);
        await _store.SetSettingAsync("playback_state", PlaybackState.Stopped.ToString());
    }

    /// <summary>
    /// Play the next song in queue.
    /// </summary>
    /// <returns></returns>
    public Task PlayNext()
    {
        return PlayNextInternal();
    }

    private async Task PlayNextInternal()
    {
        var loop = string.Equals(await _store.GetSettingAsync("loop"), "true", StringComparison.OrdinalIgnoreCase);
        if (loop)
        {
            var current = await _store.GetCurrentSongAsync();
            if (current != null)
            {
                var shuffleModeLoop = await _store.GetSettingAsync("shuffle_mode");
                await Play(
                    current,
                    string.Equals(shuffleModeLoop, "shuffle", StringComparison.OrdinalIgnoreCase),
                    loop: true,
                    smartShuffle: string.Equals(shuffleModeLoop, "smart", StringComparison.OrdinalIgnoreCase));
                return;
            }
        }

        var song = await _store.DequeueAsync();
        if (song == null)
        {
            throw new InvalidPlaybackStateException("play next", "queue is empty");
        }

        var shuffleMode = await _store.GetSettingAsync("shuffle_mode");
        await Play(
            song,
            string.Equals(shuffleMode, "shuffle", StringComparison.OrdinalIgnoreCase),
            loop,
            string.Equals(shuffleMode, "smart", StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Play the last addition to played Songs.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidPlaybackStateException"></exception>
    public Task PlayPrevious()
    {
        return PlayPreviousInternal();
    }

    private async Task PlayPreviousInternal()
    {
        var song = await _store.PopHistoryAsync();
        if (song == null)
        {
            throw new InvalidPlaybackStateException("play previous", "not been played before");
        }

        var loop = string.Equals(await _store.GetSettingAsync("loop"), "true", StringComparison.OrdinalIgnoreCase);
        var shuffleMode = await _store.GetSettingAsync("shuffle_mode");
        await Play(
            song,
            string.Equals(shuffleMode, "shuffle", StringComparison.OrdinalIgnoreCase),
            loop,
            string.Equals(shuffleMode, "smart", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Song> Search(string target, bool shuffle = false, bool smartShuffle = false)
    {
        return await ResolveTargetAsync(target, shuffle, smartShuffle);
    }

    public async Task Lyrics(bool printAll)
    {
        var current = await GetCurrentSongOrThrow();
        if (current.Source != SongSource.Spotify)
        {
            Console.WriteLine("Lyrics are currently only available for Spotify tracks.");
            return;
        }

        await _lyricsProvider.GetLyrics(current);
    }

    public async Task Add(string? target, string? playlist)
    {
        Song song;
        if (string.IsNullOrWhiteSpace(target))
        {
            song = await GetCurrentSongOrThrow();
        }
        else
        {
            song = await ResolveTargetAsync(target, false, false);
        }

        var targetPlaylist = string.IsNullOrWhiteSpace(playlist)
            ? (await _store.GetCurrentPlaylistAsync()) ?? "Recents"
            : playlist.Trim();

        await _store.AddSongToPlaylistAsync(targetPlaylist, song);
        await _store.SetCurrentPlaylistAsync(targetPlaylist);
        Console.WriteLine($"Added '{song.Title}' to playlist '{targetPlaylist}'.");
    }

    public async Task ChangeNick(string target, string nick)
    {
        var song = await _store.GetSongByNicknameAsync(target);
        if (song == null)
        {
            throw new InvalidSongParameterException($"song nickname '{target}'");
        }

        await _store.SetNicknameAsync(song.Id, song.Source, nick.Trim());
        Console.WriteLine($"Nickname '{target}' changed to '{nick}'.");
    }

    public async Task ChangePlaylist(string name)
    {
        var playlistName = name.Trim();
        if (LooksLikePlaylistIndex(playlistName))
        {
            var playlists = await _store.GetPlaylistsAsync();
            var index = int.Parse(playlistName[1..]);
            if (index < 0 || index >= playlists.Count)
            {
                throw new InvalidSongParameterException($"playlist index {playlistName}");
            }

            playlistName = playlists[index];
        }

        await _store.EnsurePlaylistAsync(playlistName);
        await _store.SetCurrentPlaylistAsync(playlistName);
        Console.WriteLine($"Current playlist set to '{playlistName}'.");
    }

    public async Task<IReadOnlyList<string>> ListPlaylists()
    {
        return await _store.GetPlaylistsAsync();
    }

    public async Task<IReadOnlyList<Song>> ListSongs(string? playlistSelector)
    {
        string playlistName;
        var playlists = await _store.GetPlaylistsAsync();
        if (string.IsNullOrWhiteSpace(playlistSelector))
        {
            playlistName = (await _store.GetCurrentPlaylistAsync()) ?? "Recents";
        }
        else
        {
            playlistName = playlistSelector.Trim();
            if (TryParsePlaylistIndex(playlistName, out var index))
            {
                if (index < 0 || index >= playlists.Count)
                {
                    throw new InvalidSongParameterException($"playlist index {playlistName}");
                }

                playlistName = playlists[index];
            }
            else if (!playlists.Contains(playlistName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidSongParameterException($"playlist '{playlistName}'");
            }
        }

        return await _store.GetPlaylistSongsAsync(playlistName);
    }

    public async Task Queue(string target)
    {
        var song = await ResolveTargetAsync(target, false, false);
        await _store.EnqueueAsync(song);
        Console.WriteLine($"Queued '{song.Title}' by {song.Artist}.");
    }

    public async Task ChangeTheme(string themeName)
    {
        var normalized = themeName.Trim().ToLowerInvariant();
        var themes = await ListThemes();
        if (!themes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidSongParameterException($"theme '{themeName}'");
        }

        await _store.SetSettingAsync("theme", normalized);
        Console.WriteLine($"Theme changed to '{normalized}'.");
    }

    public Task<IReadOnlyList<string>> ListThemes()
    {
        IReadOnlyList<string> themes = ["default", "blue", "green", "purple", "mono"];
        return Task.FromResult(themes);
    }

    public async Task<IReadOnlyList<string>> ListDevices()
    {
        return await _store.GetDevicesAsync();
    }

    public async Task ChangeDevice(string deviceName)
    {
        var normalized = deviceName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidSongParameterException("device");
        }

        await _store.AddDeviceIfMissingAsync(normalized);
        await _store.SetSettingAsync("device", normalized);
        Console.WriteLine($"Playback device set to '{normalized}'.");
    }

    public async Task SwitchSpotifyMode()
    {
        await _store.SetSettingAsync("mode", "spotify");
        Console.WriteLine("Switched to spotify-only mode.");
    }

    public async Task SwitchLocalMode()
    {
        await _store.SetSettingAsync("mode", "local");
        Console.WriteLine("Switched to local playback mode.");
    }

    public async Task SpotifyAuth()
    {
        if (_spotify is not ISpotifyAuthBackend authBackend)
        {
            throw new InvalidPlaybackStateException("authenticate spotify", "spotify backend does not support auth");
        }

        await authBackend.AuthenticateAsync();
    }
}

// -- Utils --

public enum PlaybackState
{
    Stopped,
    Paused,
    Playing
}
