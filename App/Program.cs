using CLI.Commands;
using LocalPlayer;
using TUI.Commands;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var player = new LocalPlayer.LocalPlayer();

        // Handlers for CLI and TUI
        var cliHandler = new CommandHandler(player);
        var tuiHandler = new TUIHandler(player); // assume similar to CLI handler

        Console.WriteLine("=== Master Music App ===");
        Console.WriteLine("CLI and TUI can both control playback simultaneously.");
        Console.WriteLine("Commands: play <file>, pause, resume, stop, exit");

        // Run CLI and TUI loops concurrently
        var cliTask = Task.Run(() => RunCLILoop(cliHandler));
        var tuiTask = Task.Run(() => RunTUILoop(tuiHandler));

        await Task.WhenAll(cliTask, tuiTask);
    }

    static async Task RunCLILoop(CommandHandler handler)
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            await handler.HandleAsync(input);
        }
    }

    static async Task RunTUILoop(TUIHandler handler)
    {
        while (true)
        {
            var input = handler.GetNextCommand(); // could be keypress/menu
            if (string.IsNullOrEmpty(input)) continue;

            await handler.HandleAsync(input);
        }
    }
}