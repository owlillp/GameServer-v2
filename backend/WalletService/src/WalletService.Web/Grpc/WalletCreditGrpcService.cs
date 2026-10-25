using Grpc.Core;
using Shared.Grpc.Wallet;
using Shared.SharedKernel.Errors;
using WalletService.Core.Services;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Web.Grpc;

public sealed class WalletCreditGrpcService(IWalletService walletService)
    : WalletCredit.WalletCreditBase
{
    private const string INVALID_REQUEST = "wallet.invalid_request";

    public override async Task<CreditReply> Credit(CreditRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.WalletId, out Guid walletId)
            || !Guid.TryParse(request.OperationId, out Guid operationId))
        {
            return Failed(INVALID_REQUEST, "WalletId and OperationId must be valid GUIDs");
        }

        if (request.Amount <= 0)
        {
            return Failed(INVALID_REQUEST, "Amount must be positive");
        }

        var currencyResult = Currency.Create(request.Currency);
        if (currencyResult.IsFailure)
        {
            return Failed(INVALID_REQUEST, currencyResult.Error.ToString());
        }

        if (!Enum.TryParse(request.Reason, ignoreCase: true, out LedgerReason reason)
            || !Enum.IsDefined(reason)
            || reason == LedgerReason.Unknown)
        {
            return Failed(INVALID_REQUEST, "Reason is invalid");
        }

        var operation = new WalletOperation(
            walletId,
            currencyResult.Value,
            request.Amount,
            reason,
            operationId,
            string.IsNullOrWhiteSpace(request.ReferenceId) ? null : request.ReferenceId,
            LedgerActorType.Service,
            ActorId: null,
            Comment: null);

        var result = await walletService.CreditAsync(operation, context.CancellationToken);
        if (result.IsSuccess || IsAlreadyProcessed(result.Error))
        {
            return new CreditReply { Success = true };
        }

        if (result.Error.Behavior == ErrorBehavior.Transient)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, Describe(result.Error)));
        }

        return Failed("wallet.credit.failed", Describe(result.Error));
    }

    private static bool IsAlreadyProcessed(Error error) =>
        error.Messages.Any(message => WalletErrors.OperationAlreadyProcessed.Messages
            .Any(expected => string.Equals(expected.Code, message.Code, StringComparison.Ordinal)));

    private static string Describe(Error error) =>
        error.Messages.Count == 0
            ? "Wallet operation failed"
            : string.Join("; ", error.Messages.Select(message => message.Message));

    private static CreditReply Failed(string code, string message) =>
        new() { Success = false, ErrorCode = code, ErrorMessage = message };
}
