using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Holds;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Wallet> WalletsRead { get; }

    IQueryable<LedgerEntry> LedgerRead { get; }

    IQueryable<CurrencyDefinition> CurrenciesRead { get; }

    IQueryable<Hold> HoldsRead { get; }
}
