namespace Core.Commands;

public class ChangeTheme : ICommand
{
    public async Task ExecuteAsync(PlaybackController controller)
    {
        await controller.ChangeTheme(ThemeName);
    }

    public string ThemeName { get; set; } = string.Empty;
}
