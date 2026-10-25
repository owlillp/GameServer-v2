using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;

namespace ClanWarsService.Infrastructure.Postgres.Repositories;

public sealed class RewardRepository(ClanWarsServiceDbContext dbContext) : IRewardRepository
{
    private static readonly string PENDING = nameof(RewardGrantStatus.Pending);

    public Task AddTableAsync(RewardTable table, CancellationToken cancellationToken) =>
        dbContext.RewardTables.AddAsync(table, cancellationToken).AsTask();

    public Task<RewardTable?> GetTableAsync(Guid tableId, CancellationToken cancellationToken) =>
        dbContext.RewardTables.AsNoTracking().FirstOrDefaultAsync(table => table.Id == tableId, cancellationToken);

    public async Task<IReadOnlyList<RewardTable>> ListTablesAsync(CancellationToken cancellationToken) =>
        await dbContext.RewardTables.AsNoTracking().OrderByDescending(table => table.CreatedAt).ToListAsync(cancellationToken);

    public Task AddGrantsAsync(IReadOnlyCollection<SeasonRewardGrant> grants, CancellationToken cancellationToken) =>
        dbContext.RewardGrants.AddRangeAsync(grants, cancellationToken);

    public async Task<IReadOnlyList<SeasonRewardGrant>> ClaimDueAsync(DateTime now, int limit, CancellationToken cancellationToken) =>
        await dbContext.RewardGrants
            .FromSql($"""
                SELECT * FROM clanwars.season_reward_grants
                WHERE status = {PENDING} AND next_attempt_at <= {now}
                ORDER BY next_attempt_at
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

    public Task<SeasonRewardGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken) =>
        dbContext.RewardGrants.FirstOrDefaultAsync(grant => grant.Id == grantId, cancellationToken);

    public async Task<IReadOnlyList<SeasonRewardGrant>> ListAsync(
        Guid seasonId,
        RewardGrantStatus? status,
        Guid? accountId,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.RewardGrants
            .AsNoTracking()
            .Where(grant => grant.SeasonId == seasonId
                && (status == null || grant.Status == status)
                && (accountId == null || grant.AccountId == accountId))
            .OrderBy(grant => grant.Rank)
            .ThenBy(grant => grant.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<RewardSummary> SummarizeAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        var counts = await dbContext.RewardGrants
            .Where(grant => grant.SeasonId == seasonId)
            .GroupBy(grant => grant.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(RewardGrantStatus status) => counts.FirstOrDefault(row => row.Status == status)?.Count ?? 0;

        return new RewardSummary(Count(RewardGrantStatus.Pending), Count(RewardGrantStatus.Granted), Count(RewardGrantStatus.Failed));
    }

    public async Task<IReadOnlyList<SeasonRewardGrant>> ListFailedAsync(Guid seasonId, CancellationToken cancellationToken) =>
        await dbContext.RewardGrants
            .Where(grant => grant.SeasonId == seasonId && grant.Status == RewardGrantStatus.Failed)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SeasonRewardGrant>> ListCreditedSinceAsync(DateTime since, Guid? afterId, int limit, CancellationToken cancellationToken)
    {
        IQueryable<SeasonRewardGrant> query = afterId is null
            ? dbContext.RewardGrants.FromSql($"""
                SELECT * FROM clanwars.season_reward_grants
                WHERE wallet_credited_at >= {since} AND account_id IS NOT NULL
                ORDER BY id LIMIT {limit}
                """)
            : dbContext.RewardGrants.FromSql($"""
                SELECT * FROM clanwars.season_reward_grants
                WHERE wallet_credited_at >= {since} AND account_id IS NOT NULL AND id > {afterId.Value}
                ORDER BY id LIMIT {limit}
                """);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeasonRewardGrant>> ListStuckItemsAsync(DateTime publishedBefore, int limit, CancellationToken cancellationToken) =>
        await dbContext.RewardGrants
            .Where(grant => grant.ItemsPublishedAt != null
                && grant.ItemsPublishedAt < publishedBefore
                && grant.ItemsGrantedAt == null
                && grant.Status != RewardGrantStatus.Failed)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
