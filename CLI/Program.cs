using CLI.Exceptions;
using Core;
using Core.Commands;
using SpotifyPlayer;
using LocalPlayer;

namespace CLI;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        IMusicBackend backend = new LocalPlayer.LocalPlayer();
        
        var commandFactory = new CommandFactory(backend);
        var dispatcher = new CommandDispatcher();
        var parser = new ArgumentParser();

        try
        {
            var context = parser.Parse(args);
            var command = commandFactory.Create(context);
            await dispatcher.DispatchAsync(command);
            return 0;
        }
        catch (CliException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }
}
