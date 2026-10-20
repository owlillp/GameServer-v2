using Grpc.Core;
using Shared.Grpc.Wallet;
using WalletService.Core.Services;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Holds;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Web.Grpc;

public sealed class WalletReservationGrpcService(IWalletService walletService)
    : WalletReservation.WalletReservationBase
{
    private const string INVALID_REQUEST = "wallet.invalid_request";

    public override async Task<ReserveReply> Reserve(ReserveRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.WalletId, out Guid walletId)
            || !Guid.TryParse(request.OperationId, out Guid operationId))
        {
            return FailedReserve(INVALID_REQUEST, "WalletId and OperationId must be valid GUIDs");
        }

        var currencyResult = Currency.Create(request.Currency);
        if (currencyResult.IsFailure)
        {
            return FailedReserve(INVALID_REQUEST, currencyResult.Error.ToString());
        }

        var operation = new WalletOperation(
            walletId,
            currencyResult.Value,
            request.Amount,
            LedgerReason.Purchase,
            operationId,
            string.IsNullOrWhiteSpace(request.ReferenceId) ? null : request.ReferenceId,
            LedgerActorType.Service,
            ActorId: null,
            Comment: "Purchase reservation");

        var result = await walletService.ReserveAsync(
            operation,
            HoldConstants.DefaultLifetime,
            context.CancellationToken);

        return result.IsSuccess
            ? new ReserveReply { Success = true, HoldId = result.Value.Id.ToString() }
            : FailedReserve("wallet.reserve.failed", Describe(result.Error));
    }

    public override async Task<CommitReply> Commit(CommitRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.HoldId, out Guid holdId))
        {
            return new CommitReply { Success = false, ErrorCode = INVALID_REQUEST, ErrorMessage = "HoldId must be a valid GUID" };
        }

        var result = await walletService.CommitAsync(holdId, context.CancellationToken);

        return result.IsSuccess
            ? new CommitReply { Success = true, HoldId = result.Value.Id.ToString() }
            : new CommitReply { Success = false, HoldId = request.HoldId, ErrorCode = "wallet.commit.failed", ErrorMessage = Describe(result.Error) };
    }

    public override async Task<ReleaseReply> Release(ReleaseRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.HoldId, out Guid holdId))
        {
            return new ReleaseReply { Success = false, ErrorCode = INVALID_REQUEST, ErrorMessage = "HoldId must be a valid GUID" };
        }

        var result = await walletService.ReleaseAsync(holdId, context.CancellationToken);

        return result.IsSuccess
            ? new ReleaseReply { Success = true, HoldId = result.Value.Id.ToString() }
            : new ReleaseReply { Success = false, HoldId = request.HoldId, ErrorCode = "wallet.release.failed", ErrorMessage = Describe(result.Error) };
    }

    private static string Describe(Shared.SharedKernel.Errors.Error error) =>
        error.Messages.Count == 0
            ? "Wallet operation failed"
            : string.Join("; ", error.Messages.Select(message => message.Message));

    private static ReserveReply FailedReserve(string code, string message) =>
        new() { Success = false, ErrorCode = code, ErrorMessage = message };
}
