using CommandLine;
using Core;
using Data;
using LocalPlayer;

namespace CLI;

static class Program
{
    private static string[] NormalizeArgs(string[] args)
    {
        return args.Select(a => a switch
        {
            "-ss" => "--smart_shuffle",
            "-ns" => "--no_shuffle",
            _ => a
        }).ToArray();
    }

    static async Task Main(string[] args)
    {
        var normalizedArgs = NormalizeArgs(args);

        var store = new SongService();
        var localBackend = new LocalPlayer.LocalPlayer();
        var spotifyBackend = new SpotifyPlayer.SpotifyPlayer();
        var metadataProvider = new LocalMetadataProvider(Core.Dto.SongSource.Local);

        var controller = new PlaybackController(
            localBackend,
            spotifyBackend,
            store,
            metadataProvider,
            spotifyBackend,
            Environment.CurrentDirectory);

        await controller.InitializeAsync();

        var parseResult = Parser.Default.ParseArguments(
            normalizedArgs,
            typeof(PlayOptions),
            typeof(PauseOptions),
            typeof(ResumeOptions),
            typeof(LyricsOptions),
            typeof(SearchOptions),
            typeof(QueueOptions),
            typeof(SkipOptions),
            typeof(BackOptions),
            typeof(NicknameOptions),
            typeof(AddOptions),
            typeof(ChangePlaylistOptions),
            typeof(ListPlaylistsOptions),
            typeof(ListThemesOptions),
            typeof(ChangeThemeOptions),
            typeof(SpotifyModeOptions),
            typeof(ChangeDeviceOptions),
            typeof(ListDevicesOptions));

        await parseResult
            .MapResult(
                async (object opts) =>
                {
                    var command = CommandMapper.Map(opts);
                    await command.ExecuteAsync(controller);
                },
                _ => Task.CompletedTask);
    }
}
