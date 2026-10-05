using WalletService.Contracts.Dtos;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Models;

internal static class WalletMapper
{
    public static WalletDto ToDto(Wallet wallet) =>
        new(
            wallet.Id,
            [.. wallet.Balances.Select(ToBalanceDto)],
            wallet.CreatedAt,
            wallet.UpdatedAt);

    public static BalanceDto ToBalanceDto(Balance balance) =>
        new(balance.Currency.Code, balance.Available);

    public static LedgerEntryDto ToLedgerEntryDto(LedgerEntry entry) =>
        new(
            entry.Id,
            entry.Currency.Code,
            entry.Direction.ToString(),
            entry.Amount,
            entry.Reason.ToString(),
            entry.OperationId,
            entry.ReferenceId,
            entry.ActorType.ToString(),
            entry.ActorId,
            entry.Comment,
            entry.CreatedAt);
}
