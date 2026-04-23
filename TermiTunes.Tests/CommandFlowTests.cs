using CLI;
using Core;
using Core.Commands;
using Core.Dto;
using Core.Exceptions;

namespace TermiTunes.Tests;

public class CommandFlowTests
{
    [Fact]
    public async Task Play_ExecutesAndSetsLoop()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Store.AddSongToPlaylistAsync("mix", fx.LocalSong);

        var command = new Play { Target = "mix", Loop = true, Shuffle = false };
        await command.ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.PlayCalls);
        Assert.Equal("true", await fx.Store.GetSettingAsync("loop"));
    }

    [Fact]
    public async Task Pause_PausesActiveBackend()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Controller.Play(fx.LocalSong, shuffle: false);

        await new Pause().ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.PauseCalls);
    }

    [Fact]
    public async Task Resume_ResumesActiveBackend()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Controller.Play(fx.LocalSong, shuffle: false);

        await new Resume().ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.ResumeCalls);
    }

    [Fact]
    public async Task Lyrics_CallsLyricsProviderForSpotifySong()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Controller.Play(fx.SpotifySong, shuffle: false);

        await new Lyrics { PrintAll = true }.ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LyricsProvider.Calls);
    }

    [Fact]
    public async Task Search_AutoPlayFirst_PlaysFirstMatch()
    {
        var fx = await TestFixture.CreateAsync();

        await new Search { Query = "local", AutoPlayFirst = true }.ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.PlayCalls);
    }

    [Fact]
    public async Task Queue_EnqueuesSong()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Store.SetNicknameAsync(fx.LocalSong.Id, fx.LocalSong.Source, "queued_local");

        await new QueueCommand { Track = "queued_local" }.ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.Store.QueueCount);
    }

    [Fact]
    public async Task Skip_PlaysQueuedSong()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Store.EnqueueAsync(fx.LocalSong);

        await new Skip().ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.PlayCalls);
        Assert.Equal(0, fx.Store.QueueCount);
    }

    [Fact]
    public async Task Back_PlaysSongFromHistory()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Store.PushHistoryAsync(fx.LocalSong);

        await new Back().ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.PlayCalls);
    }

    [Fact]
    public async Task ChangeNick_UpdatesNickname()
    {
        var fx = await TestFixture.CreateAsync();

        await new ChangeNick { Target = fx.LocalSong.Nickname, NewNick = "renamed_track" }.ExecuteAsync(fx.Controller);

        var song = await fx.Store.GetSongByNicknameAsync("renamed_track");
        Assert.NotNull(song);
        Assert.Equal(fx.LocalSong.Id, song!.Id);
    }

    [Fact]
    public async Task Add_AddsTargetSongToPlaylist()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Store.SetNicknameAsync(fx.LocalSong.Id, fx.LocalSong.Source, "to_add");

        await new Add { Target = "to_add", Playlist = "my_list" }.ExecuteAsync(fx.Controller);

        var songs = await fx.Store.GetPlaylistSongsAsync("my_list");
        Assert.Single(songs);
        Assert.Equal(fx.LocalSong.Id, songs[0].Id);
    }

    [Fact]
    public async Task ChangePlaylist_SetsCurrentPlaylist()
    {
        var fx = await TestFixture.CreateAsync();

        await new ChangePlaylist { Name = "roadtrip" }.ExecuteAsync(fx.Controller);

        Assert.Equal("roadtrip", await fx.Store.GetCurrentPlaylistAsync());
    }

    [Fact]
    public async Task ListPlaylists_Executes()
    {
        var fx = await TestFixture.CreateAsync();

        await new ListPlaylists().ExecuteAsync(fx.Controller);
    }

    [Fact]
    public async Task ListSongs_UsesCurrentPlaylistWhenNoArgument()
    {
        var fx = await TestFixture.CreateAsync();
        var roadtripSong = SongFactory.CreateLocal("Roadtrip Track", "Tester", TimeSpan.FromSeconds(100), "/tmp/roadtrip.mp3", []);
        await fx.Store.UpsertSongAsync(roadtripSong);
        await fx.Store.AddSongToPlaylistAsync("Roadtrip", roadtripSong);
        await fx.Store.SetCurrentPlaylistAsync("Roadtrip");

        var songs = await fx.Controller.ListSongs(null);

        Assert.Single(songs);
        Assert.Equal(roadtripSong.Id, songs[0].Id);
    }

    [Fact]
    public async Task ListSongs_InvalidIndex_Throws()
    {
        var fx = await TestFixture.CreateAsync();

        await Assert.ThrowsAsync<InvalidSongParameterException>(() => fx.Controller.ListSongs("999"));
    }

    [Fact]
    public async Task ListSongs_EmptyPlaylist_PrintsMessage()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Store.SetCurrentPlaylistAsync("EmptyPlaylist");

        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            await new ListSongs().ExecuteAsync(fx.Controller);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Contains("No songs found in the current playlist.", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListThemes_Executes()
    {
        var fx = await TestFixture.CreateAsync();

        await new ListThemes().ExecuteAsync(fx.Controller);
    }

    [Fact]
    public async Task ChangeTheme_SetsTheme()
    {
        var fx = await TestFixture.CreateAsync();

        await new ChangeTheme { ThemeName = "blue" }.ExecuteAsync(fx.Controller);

        Assert.Equal("blue", await fx.Store.GetSettingAsync("theme"));
    }

    [Fact]
    public async Task SwitchSpotifyMode_SetsMode()
    {
        var fx = await TestFixture.CreateAsync();

        await new SwitchSpotifyMode().ExecuteAsync(fx.Controller);

        Assert.Equal("spotify", await fx.Store.GetSettingAsync("mode"));
    }

    [Fact]
    public async Task SpotifyAuth_DelegatesToSpotifyBackend()
    {
        var fx = await TestFixture.CreateAsync();

        await new SpotifyAuth().ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.SpotifyBackend.AuthCalls);
    }

    [Fact]
    public async Task SwitchLocalMode_SetsMode()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Controller.SwitchSpotifyMode();

        await new SwitchLocalMode().ExecuteAsync(fx.Controller);

        Assert.Equal("local", await fx.Store.GetSettingAsync("mode"));
    }

    [Fact]
    public async Task ChangeDevice_SetsDevice()
    {
        var fx = await TestFixture.CreateAsync();

        await new ChangeDevice { DeviceName = "desk" }.ExecuteAsync(fx.Controller);

        Assert.Equal("desk", await fx.Store.GetSettingAsync("device"));
    }

    [Fact]
    public async Task ListDevices_Executes()
    {
        var fx = await TestFixture.CreateAsync();

        await new ListDevices().ExecuteAsync(fx.Controller);
    }

    [Fact]
    public async Task Stop_StopsBackendAndClearsCurrentSong()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Controller.Play(fx.LocalSong, shuffle: false);

        await new Stop().ExecuteAsync(fx.Controller);

        Assert.Equal(1, fx.LocalBackend.StopCalls);
        Assert.Null(await fx.Store.GetCurrentSongAsync());
    }

    [Fact]
    public void CommandMapper_MapsStopVerb()
    {
        var mapped = CommandMapper.Map(new StopOptions());
        Assert.IsType<Stop>(mapped);
    }

    [Fact]
    public void CommandMapper_MapsLocalVerb()
    {
        var mapped = CommandMapper.Map(new LocalModeOptions());
        Assert.IsType<SwitchLocalMode>(mapped);
    }

    [Fact]
    public void CommandMapper_MapsSpotifyAuthVerb()
    {
        var mapped = CommandMapper.Map(new SpotifyAuthOptions());
        Assert.IsType<SpotifyAuth>(mapped);
    }

    [Fact]
    public void CommandMapper_MapsListSongsVerb()
    {
        var mapped = CommandMapper.Map(new ListSongsOptions());
        Assert.IsType<ListSongs>(mapped);
    }

    [Fact]
    public async Task SpotifyMode_PreventsLocalPlayback()
    {
        var fx = await TestFixture.CreateAsync();
        await fx.Controller.SwitchSpotifyMode();

        var ex = await Assert.ThrowsAsync<BackendUnavailableException>(() => fx.Controller.Play(fx.LocalSong, false));
        Assert.Contains("local backend is unavailable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SmartShuffle_SelectsNonCurrentSongWhenPossible()
    {
        var fx = await TestFixture.CreateAsync();
        var secondLocal = SongFactory.CreateLocal("Other Local", "Artist", TimeSpan.FromSeconds(60), "/tmp/other.mp3", []);

        await fx.Store.UpsertSongAsync(secondLocal);
        await fx.Store.AddSongToPlaylistAsync("mix", fx.LocalSong);
        await fx.Store.AddSongToPlaylistAsync("mix", secondLocal);
        await fx.Store.SetCurrentSongAsync(fx.LocalSong);

        await new Play { Target = "mix", SmartShuffle = true }.ExecuteAsync(fx.Controller);

        Assert.Equal("smart", await fx.Store.GetSettingAsync("shuffle_mode"));
        Assert.Equal(secondLocal.Id, fx.LocalBackend.LastPlayed?.Id);
    }

    [Fact]
    public async Task GetSongCompletions_PrioritizesCurrentPlaylistSongs()
    {
        var fx = await TestFixture.CreateAsync();
        var preferred = SongFactory.CreateLocal("Alpha Track", "Tester", TimeSpan.FromSeconds(50), "/tmp/alpha.mp3", []);
        var other = SongFactory.CreateLocal("Alpha Other", "Tester", TimeSpan.FromSeconds(50), "/tmp/alpha2.mp3", []);

        await fx.Store.UpsertSongAsync(preferred);
        await fx.Store.UpsertSongAsync(other);
        await fx.Store.AddSongToPlaylistAsync("Recents", preferred);

        var completions = await fx.Controller.GetSongCompletions("alpha", 10);

        Assert.Equal("alpha_track", completions[0]);
        Assert.Contains("alpha_other", completions);
    }

    [Fact]
    public async Task CompleteSong_PrintsNicknameCompletions()
    {
        var fx = await TestFixture.CreateAsync();
        var song = SongFactory.CreateLocal("Beta Track", "Tester", TimeSpan.FromSeconds(50), "/tmp/beta.mp3", []);
        await fx.Store.UpsertSongAsync(song);

        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            await new CompleteSong { Query = "beta", Limit = 5 }.ExecuteAsync(fx.Controller);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Contains("beta_track", writer.ToString(), StringComparison.Ordinal);
    }

    private sealed class TestFixture
    {
        public InMemoryPlaybackStore Store { get; }
        public TrackingBackend LocalBackend { get; }
        public TrackingBackend SpotifyBackend { get; }
        public FakeMetadataProvider MetadataProvider { get; }
        public TrackingLyricsProvider LyricsProvider { get; }
        public PlaybackController Controller { get; }
        public Song LocalSong { get; }
        public Song SpotifySong { get; }

        private TestFixture(
            InMemoryPlaybackStore store,
            TrackingBackend localBackend,
            TrackingBackend spotifyBackend,
            FakeMetadataProvider metadataProvider,
            TrackingLyricsProvider lyricsProvider,
            PlaybackController controller,
            Song localSong,
            Song spotifySong)
        {
            Store = store;
            LocalBackend = localBackend;
            SpotifyBackend = spotifyBackend;
            MetadataProvider = metadataProvider;
            LyricsProvider = lyricsProvider;
            Controller = controller;
            LocalSong = localSong;
            SpotifySong = spotifySong;
        }

        public static async Task<TestFixture> CreateAsync()
        {
            var localSong = SongFactory.CreateLocal("Local Song", "Tester", TimeSpan.FromSeconds(123), "/tmp/local.mp3", []);
            var spotifySong = SongFactory.CreateSpotify("Spotify Song", "Tester", TimeSpan.FromSeconds(150), "sp-1", []);

            var store = new InMemoryPlaybackStore();
            await store.InitializeAsync();
            await store.UpsertSongAsync(localSong);
            await store.UpsertSongAsync(spotifySong);
            await store.AddSongToPlaylistAsync("Recents", localSong);
            await store.SetCurrentPlaylistAsync("Recents");

            var localBackend = new TrackingBackend();
            var spotifyBackend = new TrackingBackend();
            var metadataProvider = new FakeMetadataProvider(localSong);
            var lyricsProvider = new TrackingLyricsProvider();
            var controller = new PlaybackController(localBackend, spotifyBackend, store, metadataProvider, lyricsProvider, "/tmp");

            return new TestFixture(store, localBackend, spotifyBackend, metadataProvider, lyricsProvider, controller, localSong, spotifySong);
        }
    }

    private sealed class TrackingBackend : IMusicBackend, ISpotifyAuthBackend
    {
        public int PlayCalls { get; private set; }
        public int PauseCalls { get; private set; }
        public int ResumeCalls { get; private set; }
        public int StopCalls { get; private set; }
        public int AuthCalls { get; private set; }
        public Song? LastPlayed { get; private set; }

        public Task PlayAsync(Song song)
        {
            PlayCalls++;
            LastPlayed = song;
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            PauseCalls++;
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            ResumeCalls++;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopCalls++;
            return Task.CompletedTask;
        }

        public Task AuthenticateAsync()
        {
            AuthCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingLyricsProvider : ILyricsProvider
    {
        public int Calls { get; private set; }

        public Task GetLyrics(Song song)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMetadataProvider(Song song) : IMetadataProvider
    {
        public SongSource SupportedSource => SongSource.Local;

        public Task<Song> GetMetadataAsync(string path)
        {
            return Task.FromResult(song);
        }
    }

    private sealed class InMemoryPlaybackStore : IPlaybackStore
    {
        private readonly Dictionary<(string Id, SongSource Source), Song> _songs = new();
        private readonly Dictionary<string, (string Id, SongSource Source)> _nicknames = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<(string Id, SongSource Source)>> _playlists = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<(string Id, SongSource Source)> _queue = new();
        private readonly List<(string Id, SongSource Source)> _history = [];
        private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _devices = new(StringComparer.OrdinalIgnoreCase);
        private (string Id, SongSource Source)? _currentSong;
        private string? _currentPlaylist;

        public int QueueCount => _queue.Count;

        public Task InitializeAsync()
        {
            _playlists["Recents"] = [];
            _settings["theme"] = "default";
            _settings["mode"] = "local";
            _settings["device"] = "computer";
            _devices.Add("computer");
            return Task.CompletedTask;
        }

        public Task UpsertSongAsync(Song song)
        {
            var key = (song.Id, song.Source);
            _songs[key] = song;
            _nicknames[song.Nickname] = key;
            return Task.CompletedTask;
        }

        public Task<Song?> GetSongAsync(string songId, SongSource source)
        {
            _songs.TryGetValue((songId, source), out var song);
            return Task.FromResult(song);
        }

        public Task<Song?> GetSongByNicknameAsync(string nickname)
        {
            if (_nicknames.TryGetValue(nickname, out var key) && _songs.TryGetValue(key, out var song))
            {
                return Task.FromResult<Song?>(song);
            }

            return Task.FromResult<Song?>(null);
        }

        public Task<IReadOnlyList<Song>> SearchSongsAsync(string query, int limit = 10)
        {
            var rows = _songs.Values
                .Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                            || s.Artist.Contains(query, StringComparison.OrdinalIgnoreCase)
                            || s.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<Song>>(rows);
        }

        public Task SetNicknameAsync(string songId, SongSource source, string nickname)
        {
            var normalized = SongFactory.GenerateDefaultNickname(nickname);
            var key = (songId, source);
            _nicknames[normalized] = key;
            return Task.CompletedTask;
        }

        public Task<bool> NicknameExistsAsync(string nickname)
        {
            return Task.FromResult(_nicknames.ContainsKey(nickname));
        }

        public Task AddSongToPlaylistAsync(string playlist, Song song)
        {
            if (!_playlists.TryGetValue(playlist, out var songs))
            {
                songs = [];
                _playlists[playlist] = songs;
            }

            var key = (song.Id, song.Source);
            if (!songs.Contains(key))
            {
                songs.Add(key);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetPlaylistsAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(_playlists.Keys.OrderBy(x => x).ToList());
        }

        public Task<IReadOnlyList<Song>> GetPlaylistSongsAsync(string playlist)
        {
            if (!_playlists.TryGetValue(playlist, out var songs))
            {
                return Task.FromResult<IReadOnlyList<Song>>([]);
            }

            var resolved = songs
                .Where(_songs.ContainsKey)
                .Select(x => _songs[x])
                .ToList();
            return Task.FromResult<IReadOnlyList<Song>>(resolved);
        }

        public Task EnsurePlaylistAsync(string playlist)
        {
            if (!_playlists.ContainsKey(playlist))
            {
                _playlists[playlist] = [];
            }

            return Task.CompletedTask;
        }

        public Task SetCurrentPlaylistAsync(string playlist)
        {
            _currentPlaylist = playlist;
            if (!_playlists.ContainsKey(playlist))
            {
                _playlists[playlist] = [];
            }

            return Task.CompletedTask;
        }

        public Task<string?> GetCurrentPlaylistAsync()
        {
            return Task.FromResult(_currentPlaylist);
        }

        public Task SetCurrentSongAsync(Song? song)
        {
            _currentSong = song == null ? null : (song.Id, song.Source);
            return Task.CompletedTask;
        }

        public Task<Song?> GetCurrentSongAsync()
        {
            if (_currentSong == null)
            {
                return Task.FromResult<Song?>(null);
            }

            _songs.TryGetValue(_currentSong.Value, out var song);
            return Task.FromResult(song);
        }

        public Task EnqueueAsync(Song song)
        {
            _queue.Enqueue((song.Id, song.Source));
            return Task.CompletedTask;
        }

        public Task<Song?> DequeueAsync()
        {
            if (_queue.Count == 0)
            {
                return Task.FromResult<Song?>(null);
            }

            var key = _queue.Dequeue();
            _songs.TryGetValue(key, out var song);
            return Task.FromResult(song);
        }

        public Task PushHistoryAsync(Song song)
        {
            _history.Add((song.Id, song.Source));
            return Task.CompletedTask;
        }

        public Task<Song?> PopHistoryAsync()
        {
            if (_history.Count == 0)
            {
                return Task.FromResult<Song?>(null);
            }

            var key = _history[^1];
            _history.RemoveAt(_history.Count - 1);
            _songs.TryGetValue(key, out var song);
            return Task.FromResult(song);
        }

        public Task SetSettingAsync(string key, string value)
        {
            _settings[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetSettingAsync(string key)
        {
            _settings.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task<IReadOnlyList<string>> GetDevicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(_devices.OrderBy(x => x).ToList());
        }

        public Task AddDeviceIfMissingAsync(string deviceName)
        {
            _devices.Add(deviceName);
            return Task.CompletedTask;
        }
    }
}
