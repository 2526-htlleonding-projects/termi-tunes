using Core;
using Core.Dto;
using SpotifyPlayer.Exceptions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotifyPlayer;

/// <summary>
/// Placeholder Spotify backend.
/// This keeps the CLI functional until real Spotify API integration is implemented.
/// </summary>
public class SpotifyPlayer : IMusicBackend, ILyricsProvider, ISpotifyAuthBackend
{
    private static readonly HttpClient Http = new();
    private static readonly string[] RequiredScopes =
    [
        "user-read-private",
        "user-read-email",
        "user-modify-playback-state",
        "user-read-playback-state",
        "user-read-currently-playing"
    ];

    private readonly SpotifyAuthStorage _storage = new();

    public Task PlayAsync(Song song)
    {
        Console.WriteLine($"Spotify play: {song.Title} - {song.Artist}");
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        Console.WriteLine("Spotify pause.");
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        Console.WriteLine("Spotify resume.");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Console.WriteLine("Spotify stop.");
        return Task.CompletedTask;
    }

    public Task GetLyrics(Song song)
    {
        Console.WriteLine($"Lyrics for '{song.Title}' are not integrated yet.");
        return Task.CompletedTask;
    }

    public async Task AuthenticateAsync()
    {
        var existing = await _storage.LoadAsync();
        var clientId = PromptForClientId(existing?.ClientId);
        var codeVerifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var codeChallenge = BuildCodeChallenge(codeVerifier);
        var state = Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        var redirectUri = BuildRedirectUri();

        using var callbackListener = new HttpListener();
        callbackListener.Prefixes.Add(redirectUri.ToString());
        try
        {
            callbackListener.Start();
        }
        catch (HttpListenerException ex)
        {
            throw new SpotifyAuthException($"Unable to start local callback listener at '{redirectUri}'. {ex.Message}", ex);
        }

        var authUrl = BuildAuthorizeUrl(clientId, redirectUri, codeChallenge, state);
        Console.WriteLine("Starting Spotify authentication.");
        Console.WriteLine($"If browser opening fails, open this URL manually:\n{authUrl}");

        if (TryOpenBrowser(authUrl))
        {
            Console.WriteLine("Opened browser for Spotify consent.");
        }

        var code = await TryReceiveCallbackCodeAsync(callbackListener, state);
        if (string.IsNullOrWhiteSpace(code))
        {
            code = PromptForManualCode(state);
        }

        var token = await ExchangeAuthorizationCodeAsync(clientId, redirectUri, codeVerifier, code);
        await _storage.SaveAsync(new SpotifyAuthData
        {
            ClientId = clientId,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            TokenType = token.TokenType,
            Scope = token.Scope,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn)
        });

        Console.WriteLine($"Spotify auth saved to '{_storage.FilePath}'.");
    }

    private static string PromptForClientId(string? existingClientId)
    {
        var hasExisting = !string.IsNullOrWhiteSpace(existingClientId);
        Console.Write(hasExisting
            ? "Spotify Client ID (press Enter to keep current): "
            : "Spotify Client ID: ");

        var input = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input) && hasExisting)
        {
            return existingClientId!;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new SpotifyAuthException("Spotify Client ID is required.");
        }

        return input;
    }

    private static Uri BuildRedirectUri()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return new Uri($"http://127.0.0.1:{port}/callback/");
    }

    private static string BuildAuthorizeUrl(string clientId, Uri redirectUri, string codeChallenge, string state)
    {
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri.ToString(),
            ["code_challenge_method"] = "S256",
            ["code_challenge"] = codeChallenge,
            ["state"] = state,
            ["scope"] = string.Join(' ', RequiredScopes)
        };

        var queryString = string.Join("&", query.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        return $"https://accounts.spotify.com/authorize?{queryString}";
    }

    private static bool TryOpenBrowser(string url)
    {
        try
        {
            _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static async Task<string?> TryReceiveCallbackCodeAsync(HttpListener callbackListener, string expectedState)
    {
        Console.WriteLine("Waiting for Spotify callback (2 minutes timeout)...");
        var callbackTask = callbackListener.GetContextAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
        var completed = await Task.WhenAny(callbackTask, timeoutTask);
        if (completed != callbackTask)
        {
            Console.WriteLine("No callback received. Switching to manual URL paste fallback.");
            return null;
        }

        var context = await callbackTask;
        var code = ParseAuthCode(context.Request.Url, expectedState);
        await WriteCallbackResponseAsync(context.Response);
        return code;
    }

    private static async Task WriteCallbackResponseAsync(HttpListenerResponse response)
    {
        const string html = "<html><body><h2>Spotify login complete.</h2><p>You can close this tab and return to TermiTunes.</p></body></html>";
        var payload = Encoding.UTF8.GetBytes(html);
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = payload.Length;
        await response.OutputStream.WriteAsync(payload);
        response.Close();
    }

    private static string PromptForManualCode(string expectedState)
    {
        Console.Write("Paste the full redirected URL from your browser: ");
        var redirect = (Console.ReadLine() ?? string.Empty).Trim();
        if (!Uri.TryCreate(redirect, UriKind.Absolute, out var redirectUri))
        {
            throw new SpotifyAuthException("Invalid redirect URL provided.");
        }

        return ParseAuthCode(redirectUri, expectedState);
    }

    private static string ParseAuthCode(Uri? redirectUri, string expectedState)
    {
        if (redirectUri == null)
        {
            throw new SpotifyAuthException("Missing redirect URL from Spotify callback.");
        }

        var query = ParseQuery(redirectUri.Query);
        if (query.TryGetValue("error", out var error) && !string.IsNullOrWhiteSpace(error))
        {
            throw new SpotifyAuthException($"Spotify authorization failed: {error}");
        }

        if (!query.TryGetValue("state", out var state) || !string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            throw new SpotifyAuthException("Spotify callback state mismatch.");
        }

        if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new SpotifyAuthException("Spotify callback did not include an authorization code.");
        }

        return code;
    }

    private static Dictionary<string, string> ParseQuery(string queryString)
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = queryString.StartsWith('?') ? queryString[1..] : queryString;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return query;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var key = idx >= 0 ? pair[..idx] : pair;
            var value = idx >= 0 ? pair[(idx + 1)..] : string.Empty;
            var decodedKey = Uri.UnescapeDataString(key.Replace('+', ' '));
            var decodedValue = Uri.UnescapeDataString(value.Replace('+', ' '));
            query[decodedKey] = decodedValue;
        }

        return query;
    }

    private static async Task<SpotifyTokenResponse> ExchangeAuthorizationCodeAsync(
        string clientId,
        Uri redirectUri,
        string codeVerifier,
        string code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri.ToString(),
                ["code_verifier"] = codeVerifier
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new SpotifyAuthException($"Spotify token exchange failed ({(int)response.StatusCode}): {body}");
        }

        var token = JsonSerializer.Deserialize<SpotifyTokenResponse>(body);
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresIn <= 0)
        {
            throw new SpotifyAuthException("Spotify token exchange returned an invalid response.");
        }

        return token;
    }

    private static string BuildCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = string.Empty;
        [JsonPropertyName("scope")]
        public string Scope { get; init; } = string.Empty;
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; init; } = string.Empty;
    }
}
