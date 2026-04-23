namespace SpotifyPlayer;

public sealed class SpotifyAuthData
{
    public string ClientId { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
