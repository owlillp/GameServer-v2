using MatchmakingService.Domain.Ratings;

namespace MatchmakingService.Core.Abstractions;

public interface IRatingRepository
{
    /// <summary>
    /// Ratings of these players; players without one are not returned.
    /// </summary>
    Task<IReadOnlyList<PlayerRating>> ListAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken);

    Task AddAsync(PlayerRating rating, CancellationToken cancellationToken);
}
