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
            "play" => new Play(_musicBackend, ),
            "resume" => new Resume(_musicBackend),
            "stop" => new Stop(_musicBackend),
            _ => throw new InvalidSongParameterException("Unknown command")
        };
    }

    /// <summary>
    /// Parses a nickname to a Song using SongService
    /// </summary>
    /// <param name="nick"></param>
    /// <returns></returns>
    private Song ParseSong(string nick)
    {
        var songservice = new SongService();
        var song = songservice.GetSongReferenceAsync(nick);
    }
}

