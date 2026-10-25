using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Moves a season to the phase its schedule says. Called by the phase job at every boundary and
/// by the startup reconcile; with the row locked and the transitions computed from the clock, a
/// late, repeated or concurrent call changes nothing twice — a season whose boundaries passed
/// while the service was down catches up in one call.
/// </summary>
public sealed class SeasonLifecycle(
    ISeasonRepository seasonRepository,
    IScoreRepository scoreRepository,
    IBattleRepository battleRepository,
    RewardPlanner rewardPlanner,
    IRewardRepository rewardRepository,
    IClanWarsEventPublisher eventPublisher,
    ITransactionManager transactionManager,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider,
    ILogger<SeasonLifecycle> logger)
{
    private const int RESULTS_PAGE = 1000;

    public async Task AdvanceAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Season? season = await seasonRepository.GetForUpdateAsync(seasonId, cancellationToken);
        if (season is null)
        {
            return;
        }

        IReadOnlyList<SeasonStatus> transitions = season.Advance(now);

        if (transitions.Contains(SeasonStatus.Active))
        {
            await eventPublisher.PublishSeasonStartedAsync(season);
        }

        if (transitions.Contains(SeasonStatus.Finalizing))
        {
            // No new battles after the end; battles already running may still report during the grace.
            await battleRepository.CancelQueuedAsync(season.Id, now, cancellationToken);
        }

        if (season.IsReadyToClose(now))
        {
            // Results are frozen now: battles still without a result do not count.
            foreach (Guid battleId in await battleRepository.ListOpenAsync(season.Id, null, int.MaxValue, cancellationToken))
            {
                if (await battleRepository.TryCancelAsync(battleId, "season-closed", now, cancellationToken)
                    && await battleRepository.GetBattleAsync(battleId, cancellationToken) is { } battle)
                {
                    await eventPublisher.PublishBattleFinishedAsync(battle, BattleResultService.CANCELLED, null, now);
                }
            }

            IReadOnlyList<SeasonResult> results = await SnapshotResultsAsync(season.Id, cancellationToken);

            // Grants are created in the same transaction as the snapshot and the status change:
            // with the season row locked, closing twice cannot create them twice.
            if (season.Close(now) == SeasonStatus.Rewarding)
            {
                int grants = await rewardPlanner.PlanAsync(
                    season,
                    results,
                    now,
                    token => SaveAsync(token),
                    cancellationToken);

                if (grants == 0)
                {
                    season.CompleteRewards(now);
                }

                logger.LogInformation("Season {Number}: {Count} reward grants created", season.Number, grants);
            }

            await eventPublisher.PublishSeasonEndedAsync(season, [.. results.Take(options.CurrentValue.EndedEventTopSize)]);

            logger.LogInformation(
                "Season {Number} finalized with {Count} ranked clans, now {Status}",
                season.Number,
                results.Count,
                season.Status);
        }

        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();

        foreach (SeasonStatus status in transitions)
        {
            logger.LogInformation("Season {Number} moved to {Status}", season.Number, status);
        }
    }

    /// <summary>
    /// Rewarding → Closed once every grant is delivered. Failed grants keep the season open until
    /// an admin retries them.
    /// </summary>
    public async Task<bool> CompleteRewardsAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Season? season = await seasonRepository.GetForUpdateAsync(seasonId, cancellationToken);
        if (season is null || season.Status != SeasonStatus.Rewarding)
        {
            return false;
        }

        RewardSummary summary = await rewardRepository.SummarizeAsync(seasonId, cancellationToken);
        if (summary.Pending > 0 || summary.Failed > 0)
        {
            if (summary.Pending == 0)
            {
                logger.LogError(
                    "ALERT: season {Number} has {Failed} failed reward grants and cannot close",
                    season.Number,
                    summary.Failed);
            }

            return false;
        }

        season.CompleteRewards(timeProvider.GetUtcNow().UtcDateTime);
        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();

        logger.LogInformation("Season {Number}: all {Count} rewards delivered, season closed", season.Number, summary.Granted);
        return true;
    }

    private async Task SaveAsync(CancellationToken cancellationToken) =>
        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();

    /// <summary>
    /// Freezes the final standings from PostgreSQL (the source of truth), not from Redis.
    /// </summary>
    private async Task<IReadOnlyList<SeasonResult>> SnapshotResultsAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        var results = new List<SeasonResult>();

        while (true)
        {
            IReadOnlyList<SeasonScore> page = await scoreRepository.ListTopAsync(seasonId, results.Count, RESULTS_PAGE, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            int offset = results.Count;
            results.AddRange([.. page.Select((score, index) => new SeasonResult(seasonId, offset + index + 1, score))]);
        }

        await scoreRepository.AddResultsAsync(results, cancellationToken);
        return results;
    }
}
