using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClanWarsService.Infrastructure.Postgres.Repositories;

public sealed class SeasonRepository(ClanWarsServiceDbContext dbContext) : ISeasonRepository
{
    private static readonly SeasonStatus[] RUNNING = [SeasonStatus.Active, SeasonStatus.Finalizing, SeasonStatus.Rewarding];

    public Task<Season?> GetAsync(Guid seasonId, CancellationToken cancellationToken) =>
        dbContext.Seasons.FirstOrDefaultAsync(season => season.Id == seasonId, cancellationToken);

    public async Task<Season?> GetForUpdateAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        List<Guid> locked = await dbContext.Database
            .SqlQuery<Guid>($"""SELECT id AS "Value" FROM clanwars.seasons WHERE id = {seasonId} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        return locked.Count == 0 ? null : await GetAsync(seasonId, cancellationToken);
    }

    public async Task<IReadOnlyList<Season>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Seasons.AsNoTracking().OrderByDescending(season => season.Number).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Season>> ListOpenAsync(CancellationToken cancellationToken) =>
        await dbContext.Seasons
            .AsNoTracking()
            .Where(season => season.Status != SeasonStatus.Draft && season.Status != SeasonStatus.Closed)
            .ToListAsync(cancellationToken);

    public async Task<Season?> GetCurrentAsync(CancellationToken cancellationToken) =>
        await dbContext.Seasons
                .AsNoTracking()
                .Where(season => RUNNING.Contains(season.Status))
                .OrderByDescending(season => season.StartsAt)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.Seasons
                .AsNoTracking()
                .Where(season => season.Status == SeasonStatus.Scheduled)
                .OrderBy(season => season.StartsAt)
                .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Season>> ListByStatusAsync(SeasonStatus status, CancellationToken cancellationToken) =>
        await dbContext.Seasons.AsNoTracking().Where(season => season.Status == status).ToListAsync(cancellationToken);

    public async Task<int> NextNumberAsync(CancellationToken cancellationToken) =>
        (await dbContext.Seasons.MaxAsync(season => (int?)season.Number, cancellationToken) ?? 0) + 1;

    public Task<bool> OverlapsAsync(Guid seasonId, DateTime startsAt, DateTime finalizesAt, CancellationToken cancellationToken) =>
        dbContext.Seasons.AnyAsync(
            season => season.Id != seasonId
                && season.Status != SeasonStatus.Draft
                && season.StartsAt < finalizesAt
                && startsAt < season.FinalizesAt,
            cancellationToken);

    public Task AddAsync(Season season, CancellationToken cancellationToken) =>
        dbContext.Seasons.AddAsync(season, cancellationToken).AsTask();
}

public sealed class RosterRepository(ClanWarsServiceDbContext dbContext) : IRosterRepository
{
    public Task<ClanProjection?> GetClanAsync(Guid clanId, CancellationToken cancellationToken) =>
        dbContext.Clans.FirstOrDefaultAsync(clan => clan.ClanId == clanId, cancellationToken);

    public async Task<IReadOnlyList<ClanProjection>> ListClansAsync(IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken)
    {
        Guid[] ids = [.. clanIds];
        return await dbContext.Clans.AsNoTracking().Where(clan => ids.Contains(clan.ClanId)).ToListAsync(cancellationToken);
    }

    public Task AddClanAsync(ClanProjection clan, CancellationToken cancellationToken) =>
        dbContext.Clans.AddAsync(clan, cancellationToken).AsTask();

    public Task<RosterEntry?> GetEntryAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Roster.FirstOrDefaultAsync(entry => entry.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<RosterEntry>> ListEntriesAsync(Guid clanId, CancellationToken cancellationToken) =>
        await dbContext.Roster.AsNoTracking().Where(entry => entry.ClanId == clanId).ToListAsync(cancellationToken);

    public Task AddEntryAsync(RosterEntry entry, CancellationToken cancellationToken) =>
        dbContext.Roster.AddAsync(entry, cancellationToken).AsTask();

    public void RemoveEntry(RosterEntry entry) => dbContext.Roster.Remove(entry);
}

public sealed class ScoreRepository(ClanWarsServiceDbContext dbContext, NpgsqlDataSource dataSource) : IScoreRepository
{
    // Namespace of the per-season advisory lock: hashtext('clanwars-leaderboard') + season.
    private const int LOCK_NAMESPACE = 0x4357;

    public Task AddReceiptAsync(ScoreReceipt receipt, CancellationToken cancellationToken) =>
        dbContext.Receipts.AddAsync(receipt, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ScoreReceipt>> ListReceiptsAsync(Guid seasonId, Guid? clanId, int limit, CancellationToken cancellationToken) =>
        await dbContext.Receipts
            .AsNoTracking()
            .Where(receipt => receipt.SeasonId == seasonId && (clanId == null || receipt.ClanId == clanId))
            .OrderByDescending(receipt => receipt.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task ApplyAsync(Guid seasonId, Guid clanId, long delta, int battles, int wins, DateTime now, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO clanwars.season_scores (season_id, clan_id, points, battles, wins, updated_at)
            VALUES ({seasonId}, {clanId}, GREATEST({delta}, 0), {battles}, {wins}, {now})
            ON CONFLICT (season_id, clan_id) DO UPDATE SET
                points = GREATEST(season_scores.points + {delta}, 0),
                battles = season_scores.battles + EXCLUDED.battles,
                wins = season_scores.wins + EXCLUDED.wins,
                updated_at = CASE WHEN {delta} <> 0 THEN EXCLUDED.updated_at ELSE season_scores.updated_at END
            """,
            cancellationToken);

    public Task<SeasonScore?> GetAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) =>
        dbContext.Scores.AsNoTracking().FirstOrDefaultAsync(score => score.SeasonId == seasonId && score.ClanId == clanId, cancellationToken);

    public async Task<IReadOnlyList<SeasonScore>> ListByClansAsync(Guid seasonId, IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken)
    {
        Guid[] ids = [.. clanIds];
        return await dbContext.Scores
            .AsNoTracking()
            .Where(score => score.SeasonId == seasonId && ids.Contains(score.ClanId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeasonScore>> ListTopAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken) =>
        await dbContext.Scores
            .AsNoTracking()
            .Where(score => score.SeasonId == seasonId)
            .OrderByDescending(score => score.Points)
            .ThenBy(score => score.UpdatedAt)
            .ThenBy(score => score.ClanId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SeasonScore>> ListAfterAsync(Guid seasonId, Guid? afterClanId, int limit, CancellationToken cancellationToken)
    {
        IQueryable<SeasonScore> query = afterClanId is null
            ? dbContext.Scores.FromSql($"""
                SELECT * FROM clanwars.season_scores WHERE season_id = {seasonId}
                ORDER BY clan_id LIMIT {limit}
                """)
            : dbContext.Scores.FromSql($"""
                SELECT * FROM clanwars.season_scores WHERE season_id = {seasonId} AND clan_id > {afterClanId.Value}
                ORDER BY clan_id LIMIT {limit}
                """);

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(Guid seasonId, CancellationToken cancellationToken) =>
        dbContext.Scores.CountAsync(score => score.SeasonId == seasonId, cancellationToken);

    public Task<int> CountAheadOfAsync(SeasonScore score, CancellationToken cancellationToken) =>
        dbContext.Scores.CountAsync(
            other => other.SeasonId == score.SeasonId
                && (other.Points > score.Points || (other.Points == score.Points && other.UpdatedAt < score.UpdatedAt)),
            cancellationToken);

    public Task<bool> ReceiptExistsAsync(Guid seasonId, Guid clanId, string sourceId, CancellationToken cancellationToken) =>
        dbContext.Receipts.AnyAsync(
            receipt => receipt.SeasonId == seasonId && receipt.ClanId == clanId && receipt.SourceId == sourceId,
            cancellationToken);

    public Task ApplyMemberAsync(Guid seasonId, Guid accountId, Guid clanId, long points, int battles, int wins, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO clanwars.member_season_stats (season_id, account_id, clan_id, points, battles, wins)
            VALUES ({seasonId}, {accountId}, {clanId}, {points}, {battles}, {wins})
            ON CONFLICT (season_id, account_id, clan_id) DO UPDATE SET
                points = member_season_stats.points + EXCLUDED.points,
                battles = member_season_stats.battles + EXCLUDED.battles,
                wins = member_season_stats.wins + EXCLUDED.wins
            """,
            cancellationToken);

    public async Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsBySeasonAsync(Guid seasonId, int minBattles, CancellationToken cancellationToken) =>
        await dbContext.MemberStats
            .AsNoTracking()
            .Where(stats => stats.SeasonId == seasonId && stats.Battles >= minBattles)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) =>
        await dbContext.MemberStats
            .AsNoTracking()
            .Where(stats => stats.SeasonId == seasonId && stats.ClanId == clanId)
            .ToListAsync(cancellationToken);

    public Task AddResultsAsync(IReadOnlyCollection<SeasonResult> results, CancellationToken cancellationToken) =>
        dbContext.Results.AddRangeAsync(results, cancellationToken);

    public async Task<IAsyncDisposable?> TryAcquireRebuildLockAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        int key = seasonId.GetHashCode();

        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@ns, @key)", connection);
            command.Parameters.AddWithValue("ns", LOCK_NAMESPACE);
            command.Parameters.AddWithValue("key", key);

            if (await command.ExecuteScalarAsync(cancellationToken) is true)
            {
                return new AdvisoryLock(connection, key);
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        await connection.DisposeAsync();
        return null;
    }

    private sealed class AdvisoryLock(NpgsqlConnection connection, int key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@ns, @key)", connection);
                command.Parameters.AddWithValue("ns", LOCK_NAMESPACE);
                command.Parameters.AddWithValue("key", key);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
