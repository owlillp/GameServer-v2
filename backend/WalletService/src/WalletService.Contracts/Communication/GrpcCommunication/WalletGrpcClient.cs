using CSharpFunctionalExtensions;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Shared.Communication;
using Shared.Grpc.Wallet;
using Shared.SharedKernel.Errors;

namespace WalletService.Contracts.Communication.GrpcCommunication;

public sealed class WalletGrpcClient(
    WalletReservation.WalletReservationClient client,
    IServiceTokenProvider tokenProvider,
    IOptions<WalletServiceOptions> options) : IWalletClient
{
    private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromMilliseconds(200);

    public async Task<Result<string, Error>> ReserveAsync(
        Guid walletId,
        string currency,
        long amount,
        Guid operationId,
        string? referenceId,
        CancellationToken cancellationToken)
    {
        var request = new ReserveRequest
        {
            WalletId = walletId.ToString(),
            Currency = currency,
            Amount = amount,
            OperationId = operationId.ToString(),
            ReferenceId = referenceId ?? string.Empty,
        };

        try
        {
            ReserveReply reply = await InvokeAsync(
                callOptions => client.ReserveAsync(request, callOptions).ResponseAsync,
                cancellationToken);

            return reply.Success
                ? reply.HoldId
                : GeneralErrors.Failure(reply.ErrorMessage);
        }
        catch (RpcException exception)
        {
            return GeneralErrors.Failure($"Wallet is unavailable: {exception.Status.Detail}");
        }
    }

    public async Task<Result<bool, Error>> CommitAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var request = new CommitRequest { HoldId = holdId.ToString() };

        try
        {
            CommitReply reply = await InvokeAsync(
                callOptions => client.CommitAsync(request, callOptions).ResponseAsync,
                cancellationToken);

            if (!reply.Success)
            {
                return GeneralErrors.Failure(reply.ErrorMessage);
            }

            return true;
        }
        catch (RpcException exception)
        {
            return GeneralErrors.Failure($"Wallet is unavailable: {exception.Status.Detail}");
        }
    }

    public async Task<Result<bool, Error>> ReleaseAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var request = new ReleaseRequest { HoldId = holdId.ToString() };

        try
        {
            ReleaseReply reply = await InvokeAsync(
                callOptions => client.ReleaseAsync(request, callOptions).ResponseAsync,
                cancellationToken);

            if (!reply.Success)
            {
                return GeneralErrors.Failure(reply.ErrorMessage);
            }

            return true;
        }
        catch (RpcException exception)
        {
            return GeneralErrors.Failure($"Wallet is unavailable: {exception.Status.Detail}");
        }
    }

    private async Task<TResponse> InvokeAsync<TResponse>(
        Func<CallOptions, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        WalletServiceOptions current = options.Value;

        for (int attempt = 1; attempt <= current.MaxAttempts; attempt++)
        {
            CallOptions callOptions = await CreateCallOptionsAsync(current, cancellationToken);

            try
            {
                return await action(callOptions);
            }
            catch (RpcException exception)
                when (attempt < current.MaxAttempts && IsTransient(exception.StatusCode))
            {
                await Task.Delay(RETRY_DELAY, cancellationToken);
            }
        }

        throw new InvalidOperationException("Wallet call attempts were exhausted");
    }

    private async Task<CallOptions> CreateCallOptionsAsync(
        WalletServiceOptions current,
        CancellationToken cancellationToken)
    {
        string token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        var headers = new Metadata
        {
            { "authorization", $"Bearer {token}" },
        };

        return new CallOptions(
            headers: headers,
            deadline: DateTime.UtcNow.AddSeconds(current.DeadlineSeconds));
    }

    private static bool IsTransient(StatusCode statusCode) =>
        statusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;
}

