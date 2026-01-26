using CommandLine;
using Core;

namespace CLI;

class Program
{
    private static readonly IMusicBackend _spotify;
    private static readonly IMusicBackend _local;
    
    static async Task Main(string[] args)
    {
        // 1. Initialize your Core Controller
        var controller = new PlaybackController(_local, _spotify); 

        // 2. Parse and Map
        await Parser.Default.ParseArguments<
                PlayOptions, PauseOptions, ResumeOptions, 
                LyricsOptions, SearchOptions>(args)
            .MapResult(
                async (object opts) => 
                {
                    // Map CLI Options to Core Command
                    var command = CommandMapper.Map(opts);
                    
                    // Execute using the Controller
                    await command.ExecuteAsync(controller);
                },
                errors => Task.CompletedTask // Handle parsing errors (built-in)
            );
    }
}