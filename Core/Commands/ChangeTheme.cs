namespace Core.Commands;

public class ChangeTheme : ICommand
{
    public Task ExecuteAsync(PlaybackController controller)
    {
        throw new NotImplementedException();
    }

    public string ThemeName { get; set; }
}