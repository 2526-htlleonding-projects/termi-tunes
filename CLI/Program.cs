using CLI.Exceptions;
using Core;
using Core.Commands;
using SpotifyPlayer;
using LocalPlayer;

namespace CLI;

static class Program
{
    static Task<int> Main(string[] args)
    {
        IMusicBackend backend = new LocalPlayer.LocalPlayer();
        
        var commandFactory = new CommandFactory(backend);
        var parser = new ArgumentParser();
        var playbackController = new PlaybackController(backend, new SpotifyPlayer.SpotifyPlayer());

        try
        {
            var context = parser.Parse(args);
            var command = commandFactory.Create(context);
            return Task.FromResult(0);
        }
        catch (CliException e)
        {
            Console.Error.WriteLine(e.Message);
            return Task.FromResult(1);
        }
    }
}
