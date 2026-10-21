using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class LeaderboardRepository(MatchmakingServiceDbContext dbContext) : ILeaderboardRepository
{
    public Task<LeaderboardEntry?> GetAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries
            .FirstOrDefaultAsync(entry => entry.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.LeaderboardEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.Points)
            .ThenByDescending(entry => entry.Wins)
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

    public async Task<IReadOnlyList<LeaderboardEntry>> ListAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.LeaderboardEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.Points)
            .ThenByDescending(entry => entry.Wins)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<int> CountWithMorePointsAsync(long points, CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries.CountAsync(entry => entry.Points > points, cancellationToken);

    public Task AddAsync(LeaderboardEntry entry, CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries.AddAsync(entry, cancellationToken).AsTask();

    public Task AddRangeAsync(
        IReadOnlyCollection<LeaderboardEntry> entries,
        CancellationToken cancellationToken) =>
        dbContext.LeaderboardEntries.AddRangeAsync(entries, cancellationToken);
}
