using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Ratings;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class RatingRepository(MatchmakingServiceDbContext dbContext) : IRatingRepository
{
    public async Task<IReadOnlyList<PlayerRating>> ListAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        Guid[] ids = [.. accountIds];
        return await dbContext.PlayerRatings.Where(rating => ids.Contains(rating.AccountId)).ToListAsync(cancellationToken);
    }

    public Task AddAsync(PlayerRating rating, CancellationToken cancellationToken) =>
        dbContext.PlayerRatings.AddAsync(rating, cancellationToken).AsTask();
}
