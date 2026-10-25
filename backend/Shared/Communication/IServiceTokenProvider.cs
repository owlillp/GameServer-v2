namespace Shared.Communication;

public interface IServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Drops the cached token if it is still the rejected one (e.g. after 401 / Unauthenticated),
    /// so the next call fetches a fresh token.
    /// </summary>
    void Invalidate(string rejectedToken);
}
