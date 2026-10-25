using Grpc.Core;
using Shared.Communication;
using Shared.Grpc.Session;

namespace FakeGameServer;

/// <summary>
/// gRPC client with the game server's client_credentials token on every call.
/// </summary>
public sealed class SessionClient(
    GameServerSessions.GameServerSessionsClient client,
    IServiceTokenProvider tokenProvider)
{
    public GameServerSessions.GameServerSessionsClient Raw => client;

    public async Task<CallOptions> CallOptionsAsync(CancellationToken cancellationToken, DateTime? deadline = null)
    {
        string token = await tokenProvider.GetAccessTokenAsync(cancellationToken);

        return new CallOptions(
            headers: new Metadata { { "authorization", $"Bearer {token}" } },
            deadline: deadline,
            cancellationToken: cancellationToken);
    }

    public async Task<TResponse> UnaryAsync<TResponse>(
        Func<CallOptions, AsyncUnaryCall<TResponse>> call,
        CancellationToken cancellationToken)
    {
        CallOptions options = await CallOptionsAsync(cancellationToken, DateTime.UtcNow.AddSeconds(5));
        return await call(options).ResponseAsync;
    }
}
