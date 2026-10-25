using System.Security.Claims;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace SessionService.Web.Grpc;

/// <summary>
/// Only game server clients (client_credentials) with the session.write scope may call the gRPC API.
/// Player tokens are rejected even though they are valid for the REST API of the same service.
/// </summary>
public sealed class GameServerAuthInterceptor(IOptionsMonitor<GameServerAuthOptions> options) : Interceptor
{
    private const string SCOPE_CLAIM = "scope";
    private const string CLIENT_ID_CLAIM = "client_id";
    private const string SUBJECT_CLAIM = "sub";

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        EnsureGameServer(context);
        return continuation(request, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureGameServer(context);
        return continuation(requestStream, context);
    }

    private void EnsureGameServer(ServerCallContext context)
    {
        GameServerAuthOptions current = options.CurrentValue;
        ClaimsPrincipal user = context.GetHttpContext().User;

        string? clientId = user.FindFirst(CLIENT_ID_CLAIM)?.Value;
        string? subject = user.FindFirst(SUBJECT_CLAIM)?.Value;

        bool hasScope = user.FindAll(SCOPE_CLAIM)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(current.RequiredScope, StringComparer.Ordinal);

        if (user.Identity?.IsAuthenticated != true
            || clientId is null
            || !current.AllowedClientIds.Contains(clientId, StringComparer.Ordinal)
            || !string.Equals(subject, clientId, StringComparison.Ordinal)
            || !hasScope)
        {
            throw new RpcException(new Status(
                StatusCode.PermissionDenied,
                "A game server token with the session.write scope is required"));
        }
    }
}
