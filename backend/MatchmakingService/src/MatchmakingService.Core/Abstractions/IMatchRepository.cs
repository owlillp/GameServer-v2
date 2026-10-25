using MatchmakingService.Domain.Matches;

namespace MatchmakingService.Core.Abstractions;

public interface IMatchRepository
{
    Task<Match?> GetAsync(Guid matchId, CancellationToken cancellationToken);

    Task<Match?> GetByExternalRefAsync(string externalRef, CancellationToken cancellationToken);

    Task<Match?> GetActiveByAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Match>> ListAsync(MatchStatus? status, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Locks one expired active match with <c>FOR UPDATE SKIP LOCKED</c>; must run inside a transaction.
    /// </summary>
    Task<Guid?> ClaimExpiredActiveAsync(DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Locks one active match still waiting for a game server since before <paramref name="requestedBefore"/>
    /// with <c>FOR UPDATE SKIP LOCKED</c>; must run inside a transaction.
    /// </summary>
    Task<Guid?> ClaimAllocationTimedOutAsync(DateTime requestedBefore, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the row lock (<c>FOR UPDATE</c>) and loads the match; must run inside a transaction.
    /// </summary>
    Task<Match?> GetForUpdateAsync(Guid matchId, CancellationToken cancellationToken);

    Task AddAsync(Match match, CancellationToken cancellationToken);
}
