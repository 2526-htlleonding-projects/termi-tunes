using System.Text.Json;
using SpotifyPlayer.Exceptions;

namespace SpotifyPlayer;

internal sealed class SpotifyAuthStorage
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SpotifyAuthStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            appData = Path.Combine(home, ".config");
        }

        var directory = Path.Combine(appData, "termi-tunes");
        _filePath = Path.Combine(directory, "spotify-auth.json");
    }

    public string FilePath => _filePath;

    public async Task<SpotifyAuthData?> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<SpotifyAuthData>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new SpotifyAuthException($"Failed to read Spotify auth file '{_filePath}': {ex.Message}", ex);
        }
    }

    public async Task SaveAsync(SpotifyAuthData data)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath) ?? throw new InvalidOperationException("Invalid auth storage path.");
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SpotifyAuthException($"Failed to write Spotify auth file '{_filePath}': {ex.Message}", ex);
        }
    }
}
