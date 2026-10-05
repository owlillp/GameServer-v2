using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Features.Wallets.Models;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Queries.GetTransactions;

public sealed class GetWalletTransactionsHandler(
    IReadDbContext dbContext,
    UserScopedData userScope) : IQueryHandler<IReadOnlyList<LedgerEntryDto>, GetWalletTransactionsQuery>
{
    private const int DEFAULT_LIMIT = 50;
    private const int MAX_LIMIT = 200;

    public async Task<Result<IReadOnlyList<LedgerEntryDto>, Error>> Handle(
        GetWalletTransactionsQuery query,
        CancellationToken cancellationToken = new ())
    {
        Guid? userId = userScope.Id;
        if (userId is null)
        {
            return Error.Authentication("authentication.required", "User identity is not available");
        }

        int limit = query.Limit <= 0
            ? DEFAULT_LIMIT
            : Math.Min(query.Limit, MAX_LIMIT);

        List<LedgerEntry> entries = await dbContext.LedgerRead
            .AsNoTracking()
            .Where(entry => entry.WalletId == userId.Value)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entries.ConvertAll(WalletMapper.ToLedgerEntryDto);
    }
}
