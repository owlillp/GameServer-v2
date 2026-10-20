using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Shared.Communication;

public sealed class ServiceTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceAuthOptions> options) : IServiceTokenProvider, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return _accessToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            {
                return _accessToken;
            }

            ServiceAuthOptions auth = options.Value;
            if (!auth.IsConfigured)
            {
                throw new InvalidOperationException("ServiceAuth options are not configured");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(auth.Authority), "connect/token"))
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = auth.ClientId,
                    ["client_secret"] = auth.ClientSecret,
                    ["scope"] = string.Join(' ', auth.Scopes),
                }),
            };

            HttpClient client = httpClientFactory.CreateClient(Constants.SERVICE_AUTH_HTTP_CLIENT);
            HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException("Token endpoint returned an empty access token");
            }

            _accessToken = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token.ExpiresIn - 30, 30));
            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
