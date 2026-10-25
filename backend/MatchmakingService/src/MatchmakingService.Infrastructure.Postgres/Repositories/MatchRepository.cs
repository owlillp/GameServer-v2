using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class MatchRepository(MatchmakingServiceDbContext dbContext) : IMatchRepository
{
    public Task<Match?> GetAsync(Guid matchId, CancellationToken cancellationToken) =>
        FullMatchQuery()
            .FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);

    public Task<Match?> GetActiveByAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        FullMatchQuery()
            .Where(match => match.Status == MatchStatus.Active)
            .Where(match => match.Participants.Any(participant => participant.AccountId == accountId))
            .OrderByDescending(match => match.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Match>> ListAsync(
        MatchStatus? status,
        int limit,
        CancellationToken cancellationToken)
    {
        IQueryable<Match> query = FullMatchQuery();

        if (status is not null)
        {
            query = query.Where(match => match.Status == status);
        }

        return await query
            .OrderByDescending(match => match.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid?> ClaimExpiredActiveAsync(DateTime now, CancellationToken cancellationToken)
    {
        string status = nameof(MatchStatus.Active);

        List<Guid> ids = await dbContext.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM matchmaking.matches
                WHERE status = {status} AND ends_at <= {now}
                ORDER BY ends_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return ids.Count == 0 ? null : ids[0];
    }

    public async Task<Guid?> ClaimAllocationTimedOutAsync(
        DateTime requestedBefore,
        CancellationToken cancellationToken)
    {
        string status = nameof(MatchStatus.Active);

        List<Guid> ids = await dbContext.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM matchmaking.matches
                WHERE status = {status}
                  AND allocated_at IS NULL
                  AND allocation_requested_at < {requestedBefore}
                ORDER BY allocation_requested_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return ids.Count == 0 ? null : ids[0];
    }

    public async Task<Match?> GetForUpdateAsync(Guid matchId, CancellationToken cancellationToken)
    {
        List<Guid> locked = await dbContext.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM matchmaking.matches
                WHERE id = {matchId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        return locked.Count == 0 ? null : await GetAsync(matchId, cancellationToken);
    }

    public Task AddAsync(Match match, CancellationToken cancellationToken) =>
        dbContext.Matches.AddAsync(match, cancellationToken).AsTask();

    private IQueryable<Match> FullMatchQuery() =>
        dbContext.Matches
            .Include(match => match.Parties)
            .Include(match => match.Participants)
            .Include(match => match.Results)
            .Include(match => match.Reward)
            .ThenInclude(reward => reward!.Items)
            .AsSplitQuery();
}
