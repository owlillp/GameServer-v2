using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Commands.SpentWallet;

public sealed record SpentWalletCommand(
    Guid WalletId,
    string Currency,
    long Amount,
    string Reason,
    Guid? OperationId,
    string? ReferenceId) : ICommand;
