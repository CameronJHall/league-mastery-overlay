using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace league_mastery_overlay.League;

public sealed class LcuClient
{
    private readonly HttpClient _client;
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
            BaseAddress = new Uri($"https://127.0.0.1:{auth.Port}")
        };

        string token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"riot:{auth.Password}")
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", token);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
                return default;

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}