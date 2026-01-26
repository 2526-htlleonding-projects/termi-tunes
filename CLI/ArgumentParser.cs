using CLI.Exceptions;
using Core.Commands;

namespace CLI;

//TODO use library

public sealed class ArgumentParser
{
    public CommandContext Parse(string[] args)
    {
        if (args.Length == 0)
            throw new MissingCommandException();

        var command = args[0];
        var options = new Dictionary<string, string>();
        var flags = new HashSet<string>();

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            if (!arg.StartsWith("-"))
                throw new InvalidParameterException(arg);

            // --key=value
            if (arg.StartsWith("--") && arg.Contains('='))
            {
                var parts = arg[2..].Split('=', 2);
                options[parts[0]] = parts[1];
                continue;
            }

            // --key value
            if (arg.StartsWith("--"))
            {
                var key = arg[2..];

                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[key] = args[++i];
                }
                else
                {
                    flags.Add(key);
                }

                continue;
            }

            // -f
            if (arg.StartsWith("-"))
            {
                flags.Add(arg[1..]);
            }
        }

        return new CommandContext(command, options, flags);
    }
}
