using CSharpFunctionalExtensions;
using Grpc.Core;
using Shared.Grpc.Wallet;
using Shared.SharedKernel.Errors;

namespace WalletService.Contracts.Communication.GrpcCommunication;

public sealed class WalletGrpcClient(
    WalletReservation.WalletReservationClient client,
    WalletGrpcInvoker invoker) : IWalletClient
{
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
            ReserveReply reply = await invoker.InvokeAsync(
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
            CommitReply reply = await invoker.InvokeAsync(
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
            ReleaseReply reply = await invoker.InvokeAsync(
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
}
