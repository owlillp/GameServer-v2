using MatchmakingService.Domain.Matches;

namespace MatchmakingService.Core.Abstractions;

public interface IMatchRepository
{
    Task<Match?> GetAsync(Guid matchId, CancellationToken cancellationToken);

    Task<Match?> GetActiveByAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Match>> ListAsync(MatchStatus? status, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<Match>> ListExpiredActiveAsync(DateTime now, int limit, CancellationToken cancellationToken);

    Task AddAsync(Match match, CancellationToken cancellationToken);
}
