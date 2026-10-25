using CSharpFunctionalExtensions;
using Grpc.Core;
using Shared.Grpc.Wallet;
using Shared.SharedKernel.Errors;

namespace WalletService.Contracts.Communication.GrpcCommunication;

public sealed class WalletCreditGrpcClient(
    WalletCredit.WalletCreditClient client,
    WalletGrpcInvoker invoker) : IWalletCreditClient
{
    public async Task<Result<bool, Error>> CreditAsync(
        Guid accountId,
        string currency,
        long amount,
        Guid operationId,
        string reason,
        string? referenceId,
        CancellationToken cancellationToken)
    {
        var request = new CreditRequest
        {
            WalletId = accountId.ToString(),
            Currency = currency,
            Amount = amount,
            OperationId = operationId.ToString(),
            ReferenceId = referenceId ?? string.Empty,
            Reason = reason,
        };

        try
        {
            CreditReply reply = await invoker.InvokeAsync(
                callOptions => client.CreditAsync(request, callOptions).ResponseAsync,
                cancellationToken);

            if (!reply.Success)
            {
                return GeneralErrors.Failure(reply.ErrorMessage);
            }

            return true;
        }
        catch (RpcException exception)
        {
            return GeneralErrors.Failure($"Wallet is unavailable: {exception.Status.Detail}").AsTransient();
        }
    }
}
