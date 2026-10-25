using CSharpFunctionalExtensions;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Leaderboard;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Services;

public sealed record MatchFinalization(
    Match Match,
    IReadOnlyList<Guid> AccountIds,
    IReadOnlyList<Guid> GrantIds);

/// <summary>
/// Finishes a match exactly once: the match row is locked, leaderboard points are added with an
/// atomic UPDATE in the same transaction, and a concurrent second finalization fails on the
/// match concurrency token, rolling back its points as well.
/// </summary>
public sealed class MatchFinalizer(
    IMatchRepository matchRepository,
    IPartyRepository partyRepository,
    ILeaderboardRepository leaderboardRepository,
    ILeaderboardStore leaderboardStore,
    LeaderboardSyncSignal leaderboardSyncSignal,
    IMatchRewardRepository rewardRepository,
    IMatchmakingEventPublisher eventPublisher,
    MatchRewardDispatcher rewardDispatcher,
    RatingService ratingService,
    ITransactionManager transactionManager,
    IOptionsMonitor<MatchmakingOptions> options,
    TimeProvider timeProvider,
    ILogger<MatchFinalizer> logger)
{
    /// <summary>
    /// Locks the match, finishes it, commits and runs the post-commit side effects.
    /// </summary>
    public async Task<Result<Match, Error>> FinishAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        Match? match = await matchRepository.GetForUpdateAsync(matchId, cancellationToken);
        if (match is null)
        {
            return MatchmakingErrors.MatchNotFound;
        }

        var finishResult = await FinishLockedAsync(match, cancellationToken);
        if (finishResult.IsFailure)
        {
            return finishResult.Error;
        }

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        await CompleteAsync(finishResult.Value, cancellationToken);

        return match;
    }

    /// <summary>
    /// Must be called inside an open transaction with the match row locked.
    /// Call <see cref="CompleteAsync"/> after the commit.
    /// </summary>
    public async Task<Result<MatchFinalization, Error>> FinishLockedAsync(
        Match match,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        var finishResult = match.Finish(now);
        if (finishResult.IsFailure)
        {
            return finishResult.Error;
        }

        Guid[] accountIds = [.. match.Participants.Select(participant => participant.AccountId)];

        // Clan battles and other external matches do not change the player leaderboard.
        foreach (Guid accountId in match.IsExternal ? [] : accountIds)
        {
            MatchResult? result = match.Results.FirstOrDefault(entry => entry.AccountId == accountId);

            await leaderboardRepository.ApplyResultAsync(
                accountId,
                result?.Score ?? 0,
                result?.IsWin ?? false,
                now,
                cancellationToken);
        }

        // With game servers SessionService publishes match-completed.session; publishing it here
        // too would count the match twice in EventService.
        if (options.CurrentValue.ResultsSource == ResultsSource.Admin)
        {
            foreach (MatchResult result in match.Results)
            {
                await eventPublisher.PublishMatchCompletedAsync(match, result, cancellationToken);
            }
        }

        if (match.IsExternal)
        {
            await eventPublisher.PublishExternalCompletedAsync(match, cancellationToken);
        }

        await ratingService.ApplyAsync(match, now, cancellationToken);

        List<MatchRewardGrant> grants = CreateGrants(match, accountIds, now);
        if (grants.Count > 0)
        {
            await rewardRepository.AddRangeAsync(grants, cancellationToken);
        }

        await ReleasePartiesAsync(match, now, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        logger.LogInformation(
            "Match {MatchId} finished, winner {WinnerPartyId}, {GrantCount} rewards",
            match.Id,
            match.WinnerPartyId,
            grants.Count);

        return new MatchFinalization(match, match.IsExternal ? [] : accountIds, [.. grants.Select(grant => grant.Id)]);
    }

    /// <summary>
    /// Post-commit side effects. Failures here are repaired later: the leaderboard by the
    /// divergence check, rewards by the pending-grant sweep.
    /// </summary>
    public async Task CompleteAsync(MatchFinalization finalization, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<LeaderboardEntry> entries = await leaderboardRepository.ListByAccountsAsync(
                finalization.AccountIds,
                cancellationToken);

            await leaderboardStore.SetAsync(entries, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Failed to update Redis leaderboard for match {MatchId}",
                finalization.Match.Id);

            leaderboardSyncSignal.Request();
        }

        if (finalization.GrantIds.Count == 0)
        {
            return;
        }

        try
        {
            await rewardDispatcher.DispatchPendingAsync(finalization.GrantIds, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Reward dispatch for match {MatchId} failed, the retry sweep will pick it up",
                finalization.Match.Id);
        }
    }

    private static List<MatchRewardGrant> CreateGrants(Match match, Guid[] accountIds, DateTime now)
    {
        var grants = new List<MatchRewardGrant>();

        if (match.Reward is null)
        {
            return grants;
        }

        foreach (Guid accountId in accountIds)
        {
            bool isWinner = match.Results.FirstOrDefault(entry => entry.AccountId == accountId)?.IsWin ?? false;
            long amount = match.Reward.ParticipationAmount + (isWinner ? match.Reward.WinBonusAmount : 0);

            grants.Add(MatchRewardGrant.Create(match.Id, accountId, match.Reward, amount, now));
        }

        return grants;
    }

    private async Task ReleasePartiesAsync(Match match, DateTime now, CancellationToken cancellationToken)
    {
        Guid[] partyIds = [.. match.Parties.Select(party => party.PartyId)];
        IReadOnlyList<Party> parties = await partyRepository.ListAsync(partyIds, cancellationToken);

        foreach (Party party in parties)
        {
            var releaseResult = party.ReleaseFromMatch(now);
            if (releaseResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to release party {PartyId} from match {MatchId}: {Error}",
                    party.Id,
                    match.Id,
                    releaseResult.Error.ToString());
            }
        }
    }
}
