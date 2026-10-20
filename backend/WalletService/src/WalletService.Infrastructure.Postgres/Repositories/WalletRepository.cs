using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;
using WalletService.Core.Abstractions;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Holds;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Infrastructure.Postgres.Repositories;

public sealed class WalletRepository(
    ILogger<WalletRepository> logger,
    WalletServiceDbContext dbContext) : IWalletRepository
{
    public async Task<Result<Wallet, Error>> GetByAsync(
        Expression<Func<Wallet, bool>> expression,
        CancellationToken cancellationToken)
    {
        try
        {
            var wallet = await dbContext.Wallets
                .Include(w => w.Balances)
                .FirstOrDefaultAsync(expression, cancellationToken);

            return wallet is not null
                ? wallet
                : GeneralErrors.NotFound();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while getting wallet");
            return GeneralErrors.Failure("Process get wallet");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting wallet");
            return GeneralErrors.Failure();
        }
    }

    public Task<Result<Guid, Error>> AddAsync(Wallet wallet, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Wallets.Add(wallet);
            return Task.FromResult<Result<Guid, Error>>(wallet.Id);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while creating wallet");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure("Process create wallet"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating wallet");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure());
        }
    }

    public Task<Result<Guid, Error>> AddLedgerEntryAsync(LedgerEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.LedgerEntries.Add(entry);
            return Task.FromResult<Result<Guid, Error>>(entry.Id);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while creating ledger entry");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure("Process create ledger entry"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating ledger entry");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure());
        }
    }

    public async Task<Result<Hold, Error>> GetHoldAsync(Guid holdId, CancellationToken cancellationToken)
    {
        Hold? hold = await dbContext.Holds.FirstOrDefaultAsync(entity => entity.Id == holdId, cancellationToken);
        return hold is not null ? hold : WalletErrors.HoldNotFound;
    }

    public Task AddHoldAsync(Hold hold, CancellationToken cancellationToken)
    {
        dbContext.Holds.Add(hold);
        return Task.CompletedTask;
    }
}
