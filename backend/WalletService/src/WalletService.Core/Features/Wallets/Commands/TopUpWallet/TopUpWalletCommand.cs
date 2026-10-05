using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Commands.TopUpWallet;

public sealed record TopUpWalletCommand(
    Guid WalletId,
    string Currency,
    long Amount,
    string Reason,
    Guid? OperationId,
    string? ReferenceId) : ICommand;
