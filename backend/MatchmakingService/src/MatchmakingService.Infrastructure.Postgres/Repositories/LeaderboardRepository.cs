using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class LeaderboardRepository(
    MatchmakingServiceDbContext dbContext,
    NpgsqlDataSource dataSource) : ILeaderboardRepository
{
    // Arbitrary constant shared by all replicas: "matchmaking leaderboard rebuild".
    private const long REBUILD_LOCK_KEY = 0x4D4D_4C42;

    public Task<LeaderboardEntry?> GetAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(
        int offset,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.LeaderboardEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.Points)
            .ThenBy(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.AccountId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LeaderboardEntry>> ListByAccountsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = [.. accountIds];

        return await dbContext.LeaderboardEntries
            .AsNoTracking()
            .Where(entry => ids.Contains(entry.AccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> ListAfterAsync(
        Guid? afterAccountId,
        int limit,
        CancellationToken cancellationToken)
    {
        IQueryable<LeaderboardEntry> query = afterAccountId is null
            ? dbContext.LeaderboardEntries.FromSql($"""
                SELECT * FROM matchmaking.leaderboard_entries
                ORDER BY account_id
                LIMIT {limit}
                """)
            : dbContext.LeaderboardEntries.FromSql($"""
                SELECT * FROM matchmaking.leaderboard_entries
                WHERE account_id > {afterAccountId.Value}
                ORDER BY account_id
                LIMIT {limit}
                """);

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries.CountAsync(cancellationToken);

    public Task<int> CountAheadOfAsync(LeaderboardEntry entry, CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries.CountAsync(
            other => other.Points > entry.Points
                || (other.Points == entry.Points && other.UpdatedAt < entry.UpdatedAt),
            cancellationToken);

    public Task ApplyResultAsync(
        Guid accountId,
        long points,
        bool isWin,
        DateTime now,
        CancellationToken cancellationToken)
    {
        long delta = Math.Max(0, points);
        int wins = isWin ? 1 : 0;

        // updated_at is "when the current points were reached": it moves only when points change,
        // and it is the tie-breaker of the leaderboard.
        return dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO matchmaking.leaderboard_entries (account_id, points, wins, matches_played, updated_at)
            VALUES ({accountId}, {delta}, {wins}, 1, {now})
            ON CONFLICT (account_id) DO UPDATE SET
                points = leaderboard_entries.points + EXCLUDED.points,
                wins = leaderboard_entries.wins + EXCLUDED.wins,
                matches_played = leaderboard_entries.matches_played + 1,
                updated_at = CASE WHEN EXCLUDED.points > 0
                    THEN EXCLUDED.updated_at
                    ELSE leaderboard_entries.updated_at END
            """,
            cancellationToken);
    }

    public async Task<IAsyncDisposable?> TryAcquireRebuildLockAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", REBUILD_LOCK_KEY);

            bool acquired = await command.ExecuteScalarAsync(cancellationToken) is true;
            if (acquired)
            {
                return new AdvisoryLock(connection);
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

    private sealed class AdvisoryLock(NpgsqlConnection connection) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
                command.Parameters.AddWithValue("key", REBUILD_LOCK_KEY);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                // Closing the session releases the lock even if the unlock call failed.
                await connection.DisposeAsync();
            }
        }
    }
}
