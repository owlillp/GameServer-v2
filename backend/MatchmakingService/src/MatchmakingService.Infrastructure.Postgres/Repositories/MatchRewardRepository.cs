using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class MatchRewardRepository(MatchmakingServiceDbContext dbContext) : IMatchRewardRepository
{
    public Task<MatchRewardGrant?> GetAsync(Guid grantId, CancellationToken cancellationToken) =>
        dbContext.MatchRewardGrants
            .Include(grant => grant.Items)
            .FirstOrDefaultAsync(grant => grant.Id == grantId, cancellationToken);

    public async Task<IReadOnlyList<MatchRewardGrant>> ListByMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken) =>
        await dbContext.MatchRewardGrants
            .AsNoTracking()
            .Include(grant => grant.Items)
            .Where(grant => grant.MatchId == matchId)
            .OrderBy(grant => grant.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MatchRewardGrant>> ListPendingAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.MatchRewardGrants
            .Include(grant => grant.Items)
            .Where(grant => grant.Status == MatchRewardStatus.Pending)
            .OrderBy(grant => grant.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task AddRangeAsync(
        IReadOnlyCollection<MatchRewardGrant> grants,
        CancellationToken cancellationToken) =>
        dbContext.MatchRewardGrants.AddRangeAsync(grants, cancellationToken);
}
