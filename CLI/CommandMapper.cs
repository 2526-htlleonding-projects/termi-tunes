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
                Target = o.Target, 
                Shuffle = MapShuffle(o),
                Loop = o.Loop 
            },
            
            PauseOptions _ => new Pause(),
            
            ResumeOptions _ => new Resume(),
            
            LyricsOptions o => new Lyrics
            {
                PrintAll = o.PrintAll
            },
            
            SearchOptions o => new Search
            {
                Query = o.Query, 
                AutoPlayFirst = o.PlayFirst
            },
            
            QueueOptions o => new QueueCommand
            {
                Track = o.Track
            },
            
            SkipOptions o => new Skip(),
            
            BackOptions o => new Back(),
            
            NicknameOptions o => new ChangeNick
            {
                Target = o.Target,
                NewNick = o.NewNick
                
            },
            
            AddOptions o => new Add
            {
                Playlist = o.Playlist
            },
            
            ChangePlaylistOptions o => new ChangePlaylist
            {
                Name = o.Name
            },
            
            ListPlaylistsOptions o => new ListPlaylists(),
            
            ListThemesOptions o => new ListThemes(),
            
            ChangeThemeOptions o => new ChangeTheme
            {
                ThemeName = o.ThemeName
            },

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