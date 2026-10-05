using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Features.Wallets.Models;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Queries.GetAdminWalletTransactions;

public sealed class GetAdminWalletTransactionsHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<LedgerEntryDto>, GetAdminWalletTransactionsQuery>
{
    private const int DEFAULT_LIMIT = 50;
    private const int MAX_LIMIT = 200;

    public async Task<Result<IReadOnlyList<LedgerEntryDto>, Error>> Handle(
        GetAdminWalletTransactionsQuery query,
        CancellationToken cancellationToken = new ())
    {
        int limit = query.Limit <= 0
            ? DEFAULT_LIMIT
            : Math.Min(query.Limit, MAX_LIMIT);

        List<LedgerEntry> entries = await dbContext.LedgerRead
            .AsNoTracking()
            .Where(entry => entry.WalletId == query.WalletId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entries.ConvertAll(WalletMapper.ToLedgerEntryDto);
    }
}
