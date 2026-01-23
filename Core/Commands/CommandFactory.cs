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
            "play" => CreatePlay(ctx),
            "resume" => new Resume(_musicBackend),
            "stop" => new Stop(_musicBackend),
            _ => throw new InvalidSongParameterException("Unknown command")
        };
    }

    private ICommand CreatePlay(CommandContext ctx)
    {
        if (ctx.HasFlag("spotify-id"))
        {
            return new Play(
                _musicBackend,
                SongFactory.CreateSpotify(
                    ctx.GetOption("title"),
                    ctx.GetOption("artist"),
                    TimeSpan.Parse(ctx.GetOption("duration")),
                    ctx.GetOption("spotify-id")));
        }

        return new Play(
            _musicBackend,
            SongFactory.CreateLocal(
                ctx.GetOption("title"),
                ctx.GetOption("artist"),
                TimeSpan.Parse(ctx.GetOption("duration")),
                ctx.GetOption("path")));
    }
}

