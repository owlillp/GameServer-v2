using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Leaderboard.Queries.GetMyLeaderboard;

public sealed class GetMyLeaderboardHandler(
    ILeaderboardRepository repository,
    ILeaderboardStore store,
    ILogger<GetMyLeaderboardHandler> logger) : IQueryHandler<MyLeaderboardDto, GetMyLeaderboardQuery>
{
    public async Task<Result<MyLeaderboardDto, Error>> Handle(
        GetMyLeaderboardQuery query,
        CancellationToken cancellationToken = default)
    {
        LeaderboardEntry? entry = await repository.GetAsync(query.AccountId, cancellationToken);
        if (entry is null)
        {
            return new MyLeaderboardDto(null, 0, 0, 0);
        }

        int? rank = null;

        try
        {
            long? zeroBasedRank = await store.GetRankAsync(query.AccountId, cancellationToken);
            if (zeroBasedRank is not null)
            {
                rank = (int)zeroBasedRank.Value + 1;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Leaderboard cache is unavailable, computing rank from PostgreSQL");
        }

        rank ??= await repository.CountAheadOfAsync(entry, cancellationToken) + 1;

        return new MyLeaderboardDto(rank, entry.Points, entry.Wins, entry.MatchesPlayed);
    }
}
