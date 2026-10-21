using CSharpFunctionalExtensions;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Leaderboard;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Services;

public sealed class MatchFinalizer(
    IPartyRepository partyRepository,
    ILeaderboardRepository leaderboardRepository,
    ILeaderboardStore leaderboardStore,
    IMatchRewardRepository rewardRepository,
    IMatchmakingEventPublisher eventPublisher,
    MatchRewardDispatcher rewardDispatcher,
    ITransactionManager transactionManager,
    ILogger<MatchFinalizer> logger)
{
    public async Task<Result<Match, Error>> FinishAsync(
        Match match,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        var finishResult = match.Finish(now);
        if (finishResult.IsFailure)
        {
            return finishResult.Error;
        }

        foreach (Guid accountId in match.Participants.Select(participant => participant.AccountId))
        {
            MatchResult? result = match.Results
                .FirstOrDefault(entry => entry.AccountId == accountId);

            LeaderboardEntry? entry = await leaderboardRepository.GetAsync(
                accountId,
                cancellationToken);

            if (entry is null)
            {
                await leaderboardRepository.AddAsync(
                    LeaderboardEntry.Create(
                        accountId,
                        result?.Score ?? 0,
                        result?.IsWin ?? false,
                        now),
                    cancellationToken);
            }
            else
            {
                entry.Apply(result?.Score ?? 0, result?.IsWin ?? false, now);
            }
        }

        foreach (MatchResult result in match.Results)
        {
            await eventPublisher.PublishMatchCompletedAsync(match, result, cancellationToken);
        }

        var grants = new List<MatchRewardGrant>();

        if (match.Reward is not null)
        {
            foreach (Guid accountId in match.Participants.Select(participant => participant.AccountId))
            {
                MatchResult? result = match.Results
                    .FirstOrDefault(entry => entry.AccountId == accountId);

                bool isWinner = result?.IsWin ?? false;
                long amount = match.Reward.ParticipationAmount
                    + (isWinner ? match.Reward.WinBonusAmount : 0);

                MatchRewardGrant grant = MatchRewardGrant.Create(
                    match.Id,
                    accountId,
                    match.Reward,
                    amount,
                    now);

                grants.Add(grant);
            }

            await rewardRepository.AddRangeAsync(grants, cancellationToken);
        }

        var partyIds = match.Parties.Select(party => party.PartyId).ToArray();
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

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        foreach (MatchResult result in match.Results)
        {
            try
            {
                await leaderboardStore.AddPointsAsync(result.AccountId, result.Score, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Failed to update Redis leaderboard for account {AccountId}",
                    result.AccountId);
            }
        }

        foreach (MatchRewardGrant grant in grants)
        {
            await rewardDispatcher.DispatchAsync(grant, cancellationToken);
        }

        logger.LogInformation(
            "Match {MatchId} finished, winner {WinnerPartyId}, {GrantCount} rewards",
            match.Id,
            match.WinnerPartyId,
            grants.Count);

        return match;
    }
}
