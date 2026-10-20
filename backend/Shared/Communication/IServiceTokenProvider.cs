namespace Shared.Communication;

public interface IServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
