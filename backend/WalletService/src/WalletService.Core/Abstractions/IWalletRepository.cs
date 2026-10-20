using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Holds;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Abstractions;

public interface IWalletRepository
{
    Task<Result<Wallet, Error>> GetByAsync(
        Expression<Func<Wallet, bool>> expression,
        CancellationToken cancellationToken);

    Task<Result<Guid, Error>> AddAsync(Wallet wallet, CancellationToken cancellationToken);

    Task<Result<Guid, Error>> AddLedgerEntryAsync(LedgerEntry entry, CancellationToken cancellationToken);

    Task<Result<Hold, Error>> GetHoldAsync(Guid holdId, CancellationToken cancellationToken);

    Task AddHoldAsync(Hold hold, CancellationToken cancellationToken);
}
