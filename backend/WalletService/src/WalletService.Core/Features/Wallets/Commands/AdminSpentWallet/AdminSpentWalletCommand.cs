using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Commands.AdminSpentWallet;

public sealed record AdminSpentWalletCommand(
    Guid WalletId,
    string Currency,
    long Amount,
    string Reason,
    string Comment,
    Guid? OperationId,
    string? ReferenceId) : ICommand;
