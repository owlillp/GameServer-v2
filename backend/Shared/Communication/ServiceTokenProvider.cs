using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Shared.Communication;

/// <summary>
/// Caches a client_credentials token. The token and its expiry live in one immutable object
/// behind a volatile reference, so lock-free readers never see a new token with an old expiry.
/// </summary>
public sealed class ServiceTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceAuthOptions> options,
    TimeProvider timeProvider) : IServiceTokenProvider, IDisposable
{
    private static readonly TimeSpan REFRESH_BEFORE_EXPIRY = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile CachedToken? _cached;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (TryGetCached(out string token))
        {
            return token;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCached(out token))
            {
                return token;
            }

            CachedToken fetched = await FetchAsync(cancellationToken);
            _cached = fetched;
            return fetched.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate(string rejectedToken)
    {
        CachedToken? current = _cached;
        if (current is not null && string.Equals(current.Value, rejectedToken, StringComparison.Ordinal))
        {
            Interlocked.CompareExchange(ref _cached, null, current);
        }
    }

    public void Dispose() => _lock.Dispose();

    private bool TryGetCached(out string token)
    {
        CachedToken? cached = _cached;
        if (cached is not null && cached.ExpiresAt - REFRESH_BEFORE_EXPIRY > timeProvider.GetUtcNow())
        {
            token = cached.Value;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private async Task<CachedToken> FetchAsync(CancellationToken cancellationToken)
    {
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
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Token endpoint returned an empty access token");
        }

        return new CachedToken(token.AccessToken, timeProvider.GetUtcNow().AddSeconds(Math.Max(token.ExpiresIn, 60)));
    }

    private sealed record CachedToken(string Value, DateTimeOffset ExpiresAt);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
