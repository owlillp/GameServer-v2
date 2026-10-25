using Grpc.Core;
using Microsoft.Extensions.Options;
using Shared.Communication;

namespace WalletService.Contracts.Communication.GrpcCommunication;

/// <summary>
/// Common call policy for Wallet gRPC: service token, deadline, retry of transient statuses
/// and a single retry with a fresh token when the cached one is rejected.
/// </summary>
public sealed class WalletGrpcInvoker(
    IServiceTokenProvider tokenProvider,
    IOptions<WalletServiceOptions> options)
{
    private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromMilliseconds(200);

    public async Task<TResponse> InvokeAsync<TResponse>(
        Func<CallOptions, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        WalletServiceOptions current = options.Value;
        bool tokenRefreshed = false;
        int attempt = 0;

        while (true)
        {
            attempt++;
            string token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            CallOptions callOptions = CreateCallOptions(token, current, cancellationToken);

            try
            {
                return await action(callOptions);
            }
            catch (RpcException exception)
                when (exception.StatusCode == StatusCode.Unauthenticated && !tokenRefreshed)
            {
                tokenProvider.Invalidate(token);
                tokenRefreshed = true;
            }
            catch (RpcException exception)
                when (attempt < current.MaxAttempts && IsTransient(exception.StatusCode))
            {
                await Task.Delay(RETRY_DELAY, cancellationToken);
            }
        }
    }

    private static CallOptions CreateCallOptions(
        string token,
        WalletServiceOptions current,
        CancellationToken cancellationToken)
    {
        var headers = new Metadata
        {
            { "authorization", $"Bearer {token}" },
        };

        return new CallOptions(
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(current.DeadlineSeconds),
            cancellationToken: cancellationToken);
    }

    private static bool IsTransient(StatusCode statusCode) =>
        statusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;
}
