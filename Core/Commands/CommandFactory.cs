using Core.Dto;
using Core.Exceptions;
namespace Core.Commands;

public sealed class CommandFactory
{
    private readonly IMusicBackend _musicBackend;

    public CommandFactory(IMusicBackend backend)
    {
        _musicBackend = backend;
    }
    
    
    public ICommand Create(CommandContext ctx)
    {
        return ctx.CommandName switch
        {
            "pause" => new Pause(_musicBackend),
            //TODO handle the whole database situation
            "play" => new Play(_musicBackend, SongFactory.CreateDummy()),
            "resume" => new Resume(_musicBackend),
            "stop" => new Stop(_musicBackend),
            _ => throw new InvalidSongParameterException("Unknown command")
        };
    }
}

