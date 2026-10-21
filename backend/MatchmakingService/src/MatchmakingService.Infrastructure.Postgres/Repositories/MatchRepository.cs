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

    public async Task<IReadOnlyList<Match>> ListExpiredActiveAsync(
        DateTime now,
        int limit,
        CancellationToken cancellationToken) =>
        await FullMatchQuery()
            .Where(match => match.Status == MatchStatus.Active && match.EndsAt <= now)
            .OrderBy(match => match.EndsAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

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
