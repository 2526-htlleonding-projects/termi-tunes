using Core.Commands;

namespace Core;

public sealed class CommandDispatcher
{
    public Task DispatchAsync(ICommand command)
        => command.ExecuteAsync();
}

