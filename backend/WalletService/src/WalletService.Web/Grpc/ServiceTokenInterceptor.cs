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
        string allowedClientId = options.Value.AllowedClientId;
        if (string.IsNullOrWhiteSpace(allowedClientId))
        {
            throw new RpcException(new Status(
                StatusCode.PermissionDenied,
                "gRPC service client is not configured"));
        }

        var user = context.GetHttpContext().User;
        string? subject = user.FindFirst("sub")?.Value;
        string? clientId = user.FindFirst("client_id")?.Value;

        if (user.Identity?.IsAuthenticated != true
            || !string.Equals(clientId, allowedClientId, StringComparison.Ordinal)
            || !string.Equals(subject, clientId, StringComparison.Ordinal))
        {
            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                "Service token is required"));
        }
    }
}
