using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace WalletService.Web.Grpc;

public sealed class ServiceTokenInterceptor(IOptions<GrpcAuthOptions> options) : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        EnsureServiceCaller(context);
        return continuation(request, context);
    }

    private void EnsureServiceCaller(ServerCallContext context)
    {
        string[] allowedClientIds = [.. options.Value.AllClientIds];
        if (allowedClientIds.Length == 0)
        {
            throw new RpcException(new Status(
                StatusCode.PermissionDenied,
                "gRPC service client is not configured"));
        }

        var user = context.GetHttpContext().User;
        string? subject = user.FindFirst("sub")?.Value;
        string? clientId = user.FindFirst("client_id")?.Value;

        if (user.Identity?.IsAuthenticated != true
            || clientId is null
            || !allowedClientIds.Contains(clientId, StringComparer.Ordinal)
            || !string.Equals(subject, clientId, StringComparison.Ordinal))
        {
            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                "Service token is required"));
        }
    }
}
