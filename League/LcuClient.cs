using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace league_mastery_overlay.League;

internal sealed class LcuClient : IDisposable
{
    private readonly HttpClient _client;
    private bool _disposed = false;
    
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public LcuClient(LcuAuthInfo auth)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://127.0.0.1:{auth.Port}"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        string token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"riot:{auth.Password}")
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", token);
    }
    
    public async Task<bool> IsConnectedAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LcuClient));
            
        try
        {
            var response = await _client.GetAsync("/telemetry/v1/application-start-time");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LcuClient));
            
        try
        {
            var response = await _client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
                return default;

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LcuClient] Request failed: {ex.Message}");
            return default;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
            
        _client?.Dispose();
        _disposed = true;
    }
}