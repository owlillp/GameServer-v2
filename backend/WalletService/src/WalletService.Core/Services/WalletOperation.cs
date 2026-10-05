using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Services;

public sealed record WalletOperation(
    Guid WalletId,
    Currency Currency,
    long Amount,
    LedgerReason Reason,
    Guid OperationId,
    string? ReferenceId,
    LedgerActorType ActorType,
    Guid? ActorId,
    string? Comment);