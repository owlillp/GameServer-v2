using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Leaderboard reads: ranks from Redis, details from PostgreSQL; if Redis fails, everything from PostgreSQL.
/// </summary>
public sealed class LeaderboardReader(
    IScoreRepository scoreRepository,
    IRosterRepository rosterRepository,
    ISeasonLeaderboardStore store,
    LeaderboardSyncSignal syncSignal,
    ILogger<LeaderboardReader> logger)
{
    public async Task<IReadOnlyList<LeaderboardRowDto>> GetRangeAsync(
        Guid seasonId,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SeasonScore> scores;

        try
        {
            IReadOnlyList<LeaderboardRow> rows = await store.GetRangeAsync(seasonId, offset, limit, cancellationToken);
            IReadOnlyList<SeasonScore> details = await scoreRepository.ListByClansAsync(
                seasonId,
                [.. rows.Select(row => row.ClanId)],
                cancellationToken);

            var byClan = details.ToDictionary(score => score.ClanId);
            scores = [.. rows.Where(row => byClan.ContainsKey(row.ClanId)).Select(row => byClan[row.ClanId])];

            if (scores.Count == 0 && offset == 0 && await scoreRepository.CountAsync(seasonId, cancellationToken) > 0)
            {
                syncSignal.Request(seasonId);
                scores = await scoreRepository.ListTopAsync(seasonId, offset, limit, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Clan wars leaderboard cache is unavailable, reading PostgreSQL");
            syncSignal.Request(seasonId);
            scores = await scoreRepository.ListTopAsync(seasonId, offset, limit, cancellationToken);
        }

        return await ToRowsAsync(scores, offset, cancellationToken);
    }

    public async Task<Result<ClanStandingDto, Error>> GetStandingAsync(
        Guid seasonId,
        Guid clanId,
        int neighbors,
        CancellationToken cancellationToken)
    {
        SeasonScore? score = await scoreRepository.GetAsync(seasonId, clanId, cancellationToken);
        if (score is null)
        {
            return ClanWarsErrors.ClanNotRanked;
        }

        int rank = await GetRankAsync(seasonId, score, cancellationToken);
        int offset = Math.Max(0, rank - 1 - neighbors);

        IReadOnlyList<LeaderboardRowDto> range = await GetRangeAsync(seasonId, offset, (neighbors * 2) + 1, cancellationToken);
        LeaderboardRowDto? own = range.FirstOrDefault(row => row.ClanId == clanId);

        if (own is null)
        {
            ClanProjection? clan = await rosterRepository.GetClanAsync(clanId, cancellationToken);
            own = ToRow(rank, score, clan);
        }

        return new ClanStandingDto(seasonId, own, [.. range.Where(row => row.ClanId != clanId)]);
    }

    private async Task<int> GetRankAsync(Guid seasonId, SeasonScore score, CancellationToken cancellationToken)
    {
        try
        {
            long? rank = await store.GetRankAsync(seasonId, score.ClanId, cancellationToken);
            if (rank is not null)
            {
                return (int)rank.Value + 1;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Clan wars leaderboard cache is unavailable, ranking in PostgreSQL");
        }

        return await scoreRepository.CountAheadOfAsync(score, cancellationToken) + 1;
    }

    private async Task<IReadOnlyList<LeaderboardRowDto>> ToRowsAsync(
        IReadOnlyList<SeasonScore> scores,
        int offset,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ClanProjection> clans = await rosterRepository.ListClansAsync([.. scores.Select(score => score.ClanId)], cancellationToken);
        var byId = clans.ToDictionary(clan => clan.ClanId);

        return [.. scores.Select((score, index) => ToRow(offset + index + 1, score, byId.GetValueOrDefault(score.ClanId)))];
    }

    private static LeaderboardRowDto ToRow(int rank, SeasonScore score, ClanProjection? clan) =>
        new(rank, score.ClanId, clan?.Tag ?? "?", clan?.Name ?? string.Empty, score.Points, score.Battles, score.Wins);
}
