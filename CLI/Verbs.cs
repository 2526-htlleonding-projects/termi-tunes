using CommandLine;

namespace CLI;

// -- Playback --

[Verb("play", HelpText = "Play a song or playlist.")]
public class PlayOptions
{
    [Value(0, MetaName = "target", HelpText = "Song nickname/path/query or playlist name/#index.")]
    public string? Target { get; set; }

    [Option('s', "shuffle", HelpText = "Turn on standard shuffle.")]
    public bool Shuffle { get; set; }

    [Option("smart_shuffle", HelpText = "Turn on smart shuffle.")]
    public bool SmartShuffle { get; set; }

    [Option("no_shuffle", HelpText = "Turn off shuffle.")]
    public bool NoShuffle { get; set; }

    [Option('l', "loop", HelpText = "Toggle loop mode.")]
    public bool Loop { get; set; }
}

[Verb("pause", HelpText = "Pause current playback.")]
public class PauseOptions { }

[Verb("resume", HelpText = "Resume current playback.")]
public class ResumeOptions { }

[Verb("stop", HelpText = "Stop current playback.")]
public class StopOptions { }

[Verb("lyr", HelpText = "Display lyrics for the current song.")]
public class LyricsOptions
{
    [Option('a', "all", HelpText = "Print all lyrics at once.")]
    public bool PrintAll { get; set; }
}

// -- Queue --

[Verb("queue", HelpText = "Add a track to the queue.")]
public class QueueOptions
{
    [Value(0, Required = true, HelpText = "Song nickname/path/query.")]
    public string Track { get; set; } = string.Empty;
}

[Verb("skip", HelpText = "Skip to the next song.")]
public class SkipOptions { }

[Verb("back", HelpText = "Play the previous song.")]
public class BackOptions { }

// -- Management --

[Verb("search", HelpText = "Search for a song.")]
public class SearchOptions
{
    [Value(0, Required = true)]
    public string Query { get; set; } = string.Empty;

    [Option('f', "first", HelpText = "Automatically play the first result.")]
    public bool PlayFirst { get; set; }
}

[Verb("complete-song", Hidden = true, HelpText = "Internal song completion endpoint.")]
public class CompleteSongOptions
{
    [Value(0, Required = true)]
    public string Query { get; set; } = string.Empty;

    [Option("limit", Default = 50)]
    public int Limit { get; set; }
}

[Verb("cnick", HelpText = "Change the nickname of a song.")]
public class NicknameOptions
{
    [Value(0, Required = true, HelpText = "Current nickname")]
    public string Target { get; set; } = string.Empty;

    [Value(1, Required = true, HelpText = "New nickname")]
    public string NewNick { get; set; } = string.Empty;
}

[Verb("add", HelpText = "Add current or specified song to a playlist.")]
public class AddOptions
{
    [Value(0, HelpText = "Optional song nickname/path/query")]
    public string? Target { get; set; }

    [Value(1, HelpText = "Optional playlist name")]
    public string? Playlist { get; set; }
}

// -- Playlists and appearance --

[Verb("c", HelpText = "Enter/change playlist by name or #index.")]
public class ChangePlaylistOptions
{
    [Value(0, Required = true)]
    public string Name { get; set; } = string.Empty;
}

[Verb("l", HelpText = "List all playlists.")]
public class ListPlaylistsOptions { }

[Verb("lsongs", HelpText = "List songs in current playlist or a specified playlist name/#index.")]
public class ListSongsOptions
{
    [Value(0, HelpText = "Optional playlist name or #index.")]
    public string? Playlist { get; set; }
}

[Verb("ltheme", HelpText = "List available themes.")]
public class ListThemesOptions { }

[Verb("ctheme", HelpText = "Change the UI theme.")]
public class ChangeThemeOptions
{
    [Value(0, Required = true)]
    public string ThemeName { get; set; } = string.Empty;
}

// -- Spotify mode and devices --

[Verb("spotify", HelpText = "Switch to spotify-only mode.")]
public class SpotifyModeOptions { }

[Verb("spotify-auth", HelpText = "Initialize Spotify authentication.")]
public class SpotifyAuthOptions { }

[Verb("local", HelpText = "Switch to local playback mode.")]
public class LocalModeOptions { }

[Verb("cdevice", HelpText = "Change playback device.")]
public class ChangeDeviceOptions
{
    [Value(0, Required = true)]
    public string DeviceName { get; set; } = string.Empty;
}

[Verb("ldevice", HelpText = "List available devices.")]
public class ListDevicesOptions { }
