namespace Core;

public interface ISpotifyAuthBackend
{
    Task AuthenticateAsync();
}
