using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Commands.AdminTopUpWallet;

public sealed record AdminTopUpWalletCommand(
    Guid WalletId,
    string Currency,
    long Amount,
    string Reason,
    string Comment,
    Guid? OperationId,
    string? ReferenceId) : ICommand;
