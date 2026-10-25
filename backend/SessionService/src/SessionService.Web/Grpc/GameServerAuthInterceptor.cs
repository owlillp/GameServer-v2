using System.Security.Claims;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace SessionService.Web.Grpc;

/// <summary>
/// gRPC is for machines only. <c>GameServerSessions</c> accepts game server clients with the
/// session.write scope, <c>MatchSessions</c> accepts backend service clients with the session scope.
/// Player tokens are rejected even though they are valid for the REST API of the same service.
/// </summary>
public sealed class GameServerAuthInterceptor(
    IOptionsMonitor<GameServerAuthOptions> gameServerOptions,
    IOptionsMonitor<ServiceClientAuthOptions> serviceClientOptions) : Interceptor
{
    private const string MATCH_SESSIONS_PREFIX = "/session.v1.MatchSessions/";

    private const string SCOPE_CLAIM = "scope";
    private const string CLIENT_ID_CLAIM = "client_id";
    private const string SUBJECT_CLAIM = "sub";

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAllowed(context);
        return continuation(request, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAllowed(context);
        return continuation(requestStream, context);
    }

    private void EnsureAllowed(ServerCallContext context)
    {
        (IReadOnlyList<string> allowedClientIds, string requiredScope) =
            context.Method.StartsWith(MATCH_SESSIONS_PREFIX, StringComparison.Ordinal)
                ? (serviceClientOptions.CurrentValue.AllowedClientIds, serviceClientOptions.CurrentValue.RequiredScope)
                : (gameServerOptions.CurrentValue.AllowedClientIds, gameServerOptions.CurrentValue.RequiredScope);

        ClaimsPrincipal user = context.GetHttpContext().User;

        string? clientId = user.FindFirst(CLIENT_ID_CLAIM)?.Value;
        string? subject = user.FindFirst(SUBJECT_CLAIM)?.Value;

        bool hasScope = user.FindAll(SCOPE_CLAIM)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requiredScope, StringComparer.Ordinal);

        if (user.Identity?.IsAuthenticated != true
            || clientId is null
            || !allowedClientIds.Contains(clientId, StringComparer.Ordinal)
            || !string.Equals(subject, clientId, StringComparison.Ordinal)
            || !hasScope)
        {
            throw new RpcException(new Status(
                StatusCode.PermissionDenied,
                $"A service token with the {requiredScope} scope is required"));
        }
    }
}
