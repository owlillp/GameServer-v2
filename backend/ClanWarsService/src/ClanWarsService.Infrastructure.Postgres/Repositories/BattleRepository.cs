using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Seasons;
using Microsoft.EntityFrameworkCore;

namespace ClanWarsService.Infrastructure.Postgres.Repositories;

public sealed class BattleRepository(ClanWarsServiceDbContext dbContext) : IBattleRepository
{
    private static readonly string SCHEDULED = nameof(BattleStatus.Scheduled);
    private static readonly string IN_PROGRESS = nameof(BattleStatus.InProgress);
    private static readonly string FINISHED = nameof(BattleStatus.Finished);
    private static readonly string CANCELLED = nameof(BattleStatus.Cancelled);
    private static readonly string QUEUED = nameof(BattleQueueStatus.Queued);

    public Task AddWindowsAsync(IReadOnlyCollection<BattleWindow> windows, CancellationToken cancellationToken) =>
        dbContext.BattleWindows.AddRangeAsync(windows, cancellationToken);

    public async Task<IReadOnlyList<BattleWindow>> ListWindowsAsync(Guid seasonId, CancellationToken cancellationToken) =>
        await dbContext.BattleWindows.AsNoTracking().Where(window => window.SeasonId == seasonId).OrderBy(window => window.StartsAt).ToListAsync(cancellationToken);

    /// <summary>
    /// A season published before battle windows existed has none: battles run all season long.
    /// </summary>
    public async Task<bool> IsWindowOpenAsync(Guid seasonId, DateTime now, CancellationToken cancellationToken) =>
        !await dbContext.BattleWindows.AnyAsync(window => window.SeasonId == seasonId, cancellationToken)
        || await dbContext.BattleWindows.AnyAsync(window => window.SeasonId == seasonId && window.StartsAt <= now && now < window.EndsAt, cancellationToken);

    public Task<BattleQueueEntry?> GetQueuedAsync(Guid clanId, CancellationToken cancellationToken) =>
        dbContext.BattleQueue.FirstOrDefaultAsync(entry => entry.ClanId == clanId && entry.Status == BattleQueueStatus.Queued, cancellationToken);

    public Task AddQueueEntryAsync(BattleQueueEntry entry, CancellationToken cancellationToken) =>
        dbContext.BattleQueue.AddAsync(entry, cancellationToken).AsTask();

    public async Task<IReadOnlyList<BattleQueueEntry>> ClaimQueuedAsync(Guid seasonId, int limit, CancellationToken cancellationToken) =>
        await dbContext.BattleQueue
            .FromSql($"""
                SELECT * FROM clanwars.battle_queue
                WHERE season_id = {seasonId} AND status = {QUEUED}
                ORDER BY queued_at
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

    public Task<int> CancelQueuedAsync(Guid seasonId, DateTime now, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlAsync(
            $"""
            UPDATE clanwars.battle_queue SET status = {CANCELLED}, resolved_at = {now}
            WHERE season_id = {seasonId} AND status = {QUEUED}
            """,
            cancellationToken);

    public Task AddBattleAsync(Battle battle, CancellationToken cancellationToken) =>
        dbContext.Battles.AddAsync(battle, cancellationToken).AsTask();

    public Task<Battle?> GetBattleAsync(Guid battleId, CancellationToken cancellationToken) =>
        dbContext.Battles.AsNoTracking().FirstOrDefaultAsync(battle => battle.Id == battleId, cancellationToken);

    public Task<bool> HasOpenBattleAsync(Guid clanId, CancellationToken cancellationToken) =>
        dbContext.Battles.AnyAsync(
            battle => (battle.ClanAId == clanId || battle.ClanBId == clanId)
                && (battle.Status == BattleStatus.Scheduled || battle.Status == BattleStatus.InProgress),
            cancellationToken);

    public Task<int> CountBattlesBetweenSinceAsync(Guid clanA, Guid clanB, DateTime since, CancellationToken cancellationToken) =>
        dbContext.Battles.CountAsync(
            battle => battle.CreatedAt >= since
                && battle.Status != BattleStatus.Cancelled
                && ((battle.ClanAId == clanA && battle.ClanBId == clanB) || (battle.ClanAId == clanB && battle.ClanBId == clanA)),
            cancellationToken);

    public async Task<IReadOnlyList<Battle>> ListByClanAsync(Guid clanId, int limit, CancellationToken cancellationToken) =>
        await dbContext.Battles
            .AsNoTracking()
            .Where(battle => battle.ClanAId == clanId || battle.ClanBId == clanId)
            .OrderByDescending(battle => battle.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListOpenAsync(Guid? seasonId, DateTime? createdBefore, int limit, CancellationToken cancellationToken) =>
        await dbContext.Battles
            .AsNoTracking()
            .Where(battle => (battle.Status == BattleStatus.Scheduled || battle.Status == BattleStatus.InProgress)
                && (seasonId == null || battle.SeasonId == seasonId)
                && (createdBefore == null || battle.CreatedAt < createdBefore))
            .OrderBy(battle => battle.CreatedAt)
            .Take(limit)
            .Select(battle => battle.Id)
            .ToListAsync(cancellationToken);

    public Task<int> CountFinishedBetweenAsync(Guid seasonId, Guid clanA, Guid clanB, CancellationToken cancellationToken) =>
        dbContext.Battles.CountAsync(
            battle => battle.SeasonId == seasonId
                && battle.Status == BattleStatus.Finished
                && ((battle.ClanAId == clanA && battle.ClanBId == clanB) || (battle.ClanAId == clanB && battle.ClanBId == clanA)),
            cancellationToken);

    public Task AddSignalsAsync(IReadOnlyCollection<AbuseSignal> signals, CancellationToken cancellationToken) =>
        dbContext.AbuseSignals.AddRangeAsync(signals, cancellationToken);

    public async Task<IReadOnlyList<AbuseSignal>> ListSignalsAsync(Guid? seasonId, int limit, CancellationToken cancellationToken) =>
        await dbContext.AbuseSignals
            .AsNoTracking()
            .Where(signal => seasonId == null || signal.SeasonId == seasonId)
            .OrderByDescending(signal => signal.DetectedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryStartAsync(Guid battleId, Guid matchId, DateTime now, CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            UPDATE clanwars.battles SET status = {IN_PROGRESS}, match_id = {matchId}, started_at = {now}
            WHERE id = {battleId} AND status = {SCHEDULED}
            """,
            cancellationToken) == 1;

    public async Task<bool> TryFinishAsync(Guid battleId, Guid? matchId, BattleOutcome outcome, DateTime now, CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            UPDATE clanwars.battles SET
                status = {FINISHED},
                match_id = COALESCE({matchId}, match_id),
                winner_clan_id = {outcome.WinnerClanId},
                points_a = {outcome.PointsA},
                points_b = {outcome.PointsB},
                finished_at = {now}
            WHERE id = {battleId} AND status IN ({SCHEDULED}, {IN_PROGRESS})
            """,
            cancellationToken) == 1;

    public async Task<bool> TryCancelAsync(Guid battleId, string reason, DateTime now, CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            UPDATE clanwars.battles SET status = {CANCELLED}, cancel_reason = {reason}, finished_at = {now}
            WHERE id = {battleId} AND status IN ({SCHEDULED}, {IN_PROGRESS})
            """,
            cancellationToken) == 1;
}
