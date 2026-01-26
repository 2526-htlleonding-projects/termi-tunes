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

            _ => throw new ArgumentException("Unknown command type", nameof(result))
        };
    }

    private static bool MapShuffle(PlayOptions o)
    {
        // Logic for your specific shuffle flags:
        // -ns (NoShuffle) takes priority, then -ss (Smart), then -s (Standard)
        if (o.NoShuffle) return false;
        if (o.SmartShuffle || o.Shuffle) return true;
        return false; 
    }
}