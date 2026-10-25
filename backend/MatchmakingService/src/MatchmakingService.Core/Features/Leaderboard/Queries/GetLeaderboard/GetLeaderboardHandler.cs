using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Core.Services;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Leaderboard.Queries.GetLeaderboard;

public sealed class GetLeaderboardHandler(
    ILeaderboardRepository repository,
    ILeaderboardStore store,
    LeaderboardSyncSignal syncSignal,
    ILogger<GetLeaderboardHandler> logger) : IQueryHandler<IReadOnlyList<LeaderboardEntryDto>, GetLeaderboardQuery>
{
    public async Task<Result<IReadOnlyList<LeaderboardEntryDto>, Error>> Handle(
        GetLeaderboardQuery query,
        CancellationToken cancellationToken = default)
    {
        int limit = Math.Clamp(query.Limit, 1, MatchmakingConstants.MAX_LEADERBOARD_LIMIT);

        try
        {
            IReadOnlyList<LeaderboardRow> rows = await store.GetTopAsync(limit, cancellationToken);
            if (rows.Count > 0)
            {
                Guid[] accountIds = [.. rows.Select(row => row.AccountId)];
                IReadOnlyList<LeaderboardEntry> entries = await repository.ListByAccountsAsync(
                    accountIds,
                    cancellationToken);

                var byAccount = entries.ToDictionary(entry => entry.AccountId);

                return rows
                    .Select((row, index) =>
                        byAccount.TryGetValue(row.AccountId, out LeaderboardEntry? entry)
                            ? entry.ToDto(index + 1)
                            : new LeaderboardEntryDto(index + 1, row.AccountId, row.Points, 0, 0))
                    .ToList();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Leaderboard cache is unavailable, falling back to PostgreSQL");
            syncSignal.Request();
        }

        IReadOnlyList<LeaderboardEntry> fallback = await repository.GetTopAsync(0, limit, cancellationToken);

        return fallback
            .Select((entry, index) => entry.ToDto(index + 1))
            .ToList();
    }
}
