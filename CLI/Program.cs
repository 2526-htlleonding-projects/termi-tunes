using CommandLine;
using Core;
using Core.Dto;
using Core.Exceptions;
using Data;
using Data.Exceptions;
using LocalPlayer;
using LocalPlayer.Exceptions;
using CLI.Exceptions;
using SpotifyPlayer.Exceptions;

namespace CLI;

static class Program
{
    private static readonly Type[] OptionTypes =
    [
        typeof(PlayOptions),
        typeof(PauseOptions),
        typeof(ResumeOptions),
        typeof(StopOptions),
        typeof(LyricsOptions),
        typeof(SearchOptions),
        typeof(CompleteSongOptions),
        typeof(QueueOptions),
        typeof(SkipOptions),
        typeof(BackOptions),
        typeof(NicknameOptions),
        typeof(AddOptions),
        typeof(ChangePlaylistOptions),
        typeof(ListPlaylistsOptions),
        typeof(ListSongsOptions),
        typeof(ListThemesOptions),
        typeof(ChangeThemeOptions),
        typeof(SpotifyModeOptions),
        typeof(SpotifyAuthOptions),
        typeof(LocalModeOptions),
        typeof(ChangeDeviceOptions),
        typeof(ListDevicesOptions)
    ];

    private static string[] NormalizeArgs(string[] args)
    {
        var normalized = args.Select(a => a switch
        {
            "-ss" => "--smart_shuffle",
            "-ns" => "--no_shuffle",
            _ => a
        }).ToArray();

        if (normalized.Length == 0)
        {
            return normalized;
        }

        normalized[0] = normalized[0] switch
        {
            "p" => "play",
            "h" => "pause",
            "r" => "resume",
            "s" => "stop",
            _ => normalized[0]
        };

        return normalized;
    }

    static async Task<int> Main(string[] args)
    {
        try
        {
            var normalizedArgs = NormalizeArgs(args);
            var parseResult = Parser.Default.ParseArguments(normalizedArgs, OptionTypes);

            return await parseResult
                .MapResult(
                    async (object opts) =>
                    {
                        var completionOnly = opts is CompleteSongOptions;
                        var controller = await BuildControllerAsync(completionOnly);
                        var command = CommandMapper.Map(opts);
                        await command.ExecuteAsync(controller);
                        return 0;
                    },
                    _ => Task.FromResult(1));
        }
        catch (Exception ex) when (ex is CliException or PlaybackException or DatabaseException or LocalPlayerException or SpotifyPlayerException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected failure: {ex.Message}");
            return 1;
        }
    }

    private static async Task<PlaybackController> BuildControllerAsync(bool completionOnly)
    {
        var store = new SongService();
        var spotifyBackend = new SpotifyPlayer.SpotifyPlayer();
        IMusicBackend localBackend;
        IMetadataProvider metadataProvider;
        ILyricsProvider lyricsProvider;

        if (completionOnly)
        {
            localBackend = new NoOpMusicBackend();
            metadataProvider = new NoOpMetadataProvider();
            lyricsProvider = new NoOpLyricsProvider();
        }
        else
        {
            localBackend = new LocalPlayer.LocalPlayer();
            metadataProvider = new LocalMetadataProvider(SongSource.Local);
            lyricsProvider = spotifyBackend;
        }

        var controller = new PlaybackController(
            localBackend,
            spotifyBackend,
            store,
            metadataProvider,
            lyricsProvider,
            Environment.CurrentDirectory);
        await controller.InitializeAsync();
        return controller;
    }

    private sealed class NoOpMusicBackend : IMusicBackend
    {
        public Task PlayAsync(Song song) => throw new InvalidOperationException("Playback is unavailable in completion mode.");
        public Task PauseAsync() => throw new InvalidOperationException("Playback is unavailable in completion mode.");
        public Task ResumeAsync() => throw new InvalidOperationException("Playback is unavailable in completion mode.");
        public Task StopAsync() => throw new InvalidOperationException("Playback is unavailable in completion mode.");
    }

    private sealed class NoOpMetadataProvider : IMetadataProvider
    {
        public SongSource SupportedSource => SongSource.Local;

        public Task<Song> GetMetadataAsync(string path)
            => throw new InvalidOperationException("Metadata lookup is unavailable in completion mode.");
    }

    private sealed class NoOpLyricsProvider : ILyricsProvider
    {
        public Task GetLyrics(Song song) => Task.CompletedTask;
    }
}
