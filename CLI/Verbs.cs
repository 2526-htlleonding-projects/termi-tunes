namespace CLI;

using CommandLine;

// -- Playback --

[Verb("play", HelpText = "Play a song or playlist.")]
public class PlayOptions {
    [Value(0, MetaName = "target", HelpText = "Song or playlist name/URI.")]
    public string Target { get; set; }

    [Option('s', "shuffle", HelpText = "Turn on standard shuffle.")]
    public bool Shuffle { get; set; }

    [Option('m', "smart_shuffle", HelpText = "Turn on smart shuffle.")]
    public bool SmartShuffle { get; set; }

    [Option('r', "no_shuffle", HelpText = "Turn off shuffle.")]
    public bool NoShuffle { get; set; }

    [Option('l', "loop", HelpText = "Toggle loop mode.")]
    public bool Loop { get; set; }
}

[Verb("pause", HelpText = "Pause current playback.")]
public class PauseOptions { }

[Verb("resume", HelpText = "Resume current playback.")]
public class ResumeOptions { }

[Verb("lyr", HelpText = "Display lyrics for the current song.")]
public class LyricsOptions {
    [Option('a', "all", HelpText = "Print all lyrics at once.")]
    public bool PrintAll { get; set; }
}

// -- Queue --

[Verb("queue", HelpText = "Add a track to the queue.")]
public class QueueOptions {
    [Value(0, Required = true)]
    public string Track { get; set; }
}

[Verb("skip", HelpText = "Skip to the next song.")]
public class SkipOptions { }

[Verb("back", HelpText = "Play the previous song.")]
public class BackOptions { }

// -- Management --

[Verb("search", HelpText = "Search for a song.")]
public class SearchOptions {
    [Value(0, Required = true)]
    public string Query { get; set; }

    [Option('f', "first", HelpText = "Automatically play the first result.")]
    public bool PlayFirst { get; set; }
}

[Verb("cnick", HelpText = "Change the nickname of a song.")]
public class NicknameOptions {
    [Value(0, HelpText = "Current name/ID")] public string Target { get; set; }
    [Value(1, HelpText = "New nickname")] public string NewNick { get; set; }
}

[Verb("add", HelpText = "Add current or specified song to a playlist.")]
public class AddOptions {
    [Value(0, Required = true, HelpText = "Playlist name")]
    public string Playlist { get; set; }
}

// -- Playlists and Appearance --

[Verb("c", HelpText = "Enter a playlist.")]
public class ChangePlaylistOptions {
    [Value(0, Required = true)]
    public string Name { get; set; }
}

[Verb("l", HelpText = "List all playlists.")]
public class ListPlaylistsOptions { }

[Verb("ltheme", HelpText = "List available themes.")]
public class ListThemesOptions { }

[Verb("ctheme", HelpText = "Change the UI theme.")]
public class ChangeThemeOptions {
    [Value(0, Required = true)]
    public string ThemeName { get; set; }
}