using CLI.Exceptions;
using Core.Commands;

namespace CLI;

public static class CommandMapper
{
    public static ICommand Map(object result)
    {
        return result switch
        {
            PlayOptions o => new Play
            {
                Target = o.Target ?? string.Empty,
                Shuffle = MapShuffle(o),
                Loop = o.Loop
            },
            PauseOptions _ => new Pause(),
            ResumeOptions _ => new Resume(),
            LyricsOptions o => new Lyrics { PrintAll = o.PrintAll },
            SearchOptions o => new Search { Query = o.Query, AutoPlayFirst = o.PlayFirst },
            QueueOptions o => new QueueCommand { Track = o.Track },
            SkipOptions _ => new Skip(),
            BackOptions _ => new Back(),
            NicknameOptions o => new ChangeNick { Target = o.Target, NewNick = o.NewNick },
            AddOptions o => new Add { Target = o.Target, Playlist = o.Playlist },
            ChangePlaylistOptions o => new ChangePlaylist { Name = o.Name },
            ListPlaylistsOptions _ => new ListPlaylists(),
            ListThemesOptions _ => new ListThemes(),
            ChangeThemeOptions o => new ChangeTheme { ThemeName = o.ThemeName },
            SpotifyModeOptions _ => new SwitchSpotifyMode(),
            ChangeDeviceOptions o => new ChangeDevice { DeviceName = o.DeviceName },
            ListDevicesOptions _ => new ListDevices(),
            _ => throw new InvalidCommandExceptions(result.GetType().Name)
        };
    }

    private static bool MapShuffle(PlayOptions o)
    {
        if (o.NoShuffle) return false;
        if (o.SmartShuffle || o.Shuffle) return true;
        return false;
    }
}
