namespace Core.Commands;

public sealed class CommandContext
{
    public string CommandName { get; }
    public Dictionary<string, string> Options { get; }
    public HashSet<string> Flags { get; }

    public CommandContext(
        string commandName,
        Dictionary<string, string> options,
        HashSet<string> flags)
    {
        CommandName = commandName;
        Options = options;
        Flags = flags;
    }

    public bool HasFlag(string name) => Flags.Contains(name);
    public string? GetOption(string name)
        => Options.TryGetValue(name, out var value) ? value : null;
}
