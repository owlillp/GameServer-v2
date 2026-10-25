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

    public async Task<IReadOnlyList<MatchRewardGrant>> ClaimPendingAsync(
        IReadOnlyCollection<Guid>? grantIds,
        int limit,
        CancellationToken cancellationToken)
    {
        string status = nameof(MatchRewardStatus.Pending);

        IQueryable<Guid> query = grantIds is null
            ? dbContext.Database.SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM matchmaking.match_reward_grants
                WHERE status = {status}
                ORDER BY created_at
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            : dbContext.Database.SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM matchmaking.match_reward_grants
                WHERE status = {status} AND id = ANY({grantIds.ToArray()})
                ORDER BY created_at
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """);

        List<Guid> claimed = await query.ToListAsync(cancellationToken);

        if (claimed.Count == 0)
        {
            return [];
        }

        return await dbContext.MatchRewardGrants
            .Include(grant => grant.Items)
            .Where(grant => claimed.Contains(grant.Id))
            .OrderBy(grant => grant.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(
        IReadOnlyCollection<MatchRewardGrant> grants,
        CancellationToken cancellationToken) =>
        dbContext.MatchRewardGrants.AddRangeAsync(grants, cancellationToken);
}
