using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Services;

public sealed record ScoreChange(
    Guid SeasonId,
    Guid ClanId,
    string SourceId,
    long Amount,
    string Reason,
    Guid? ActorId,
    int Battles = 0,
    int Wins = 0);

/// <summary>
/// The only way to change season points: a receipt unique by (season, clan, source) and an atomic
/// upsert in one transaction, with the season row locked so a closing season cannot take points
/// after its results were frozen. Redis then gets the absolute value read back from PostgreSQL.
/// </summary>
public sealed class ScoreService(
    ISeasonRepository seasonRepository,
    IRosterRepository rosterRepository,
    IScoreRepository scoreRepository,
    ISeasonLeaderboardStore leaderboardStore,
    LeaderboardSyncSignal syncSignal,
    ITransactionManager transactionManager,
    TimeProvider timeProvider,
    ILogger<ScoreService> logger)
{
    public async Task<Result<ScoreReceipt, Error>> ApplyAsync(ScoreChange change, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        ClanProjection? clan = await rosterRepository.GetClanAsync(change.ClanId, cancellationToken);
        if (clan is null || clan.IsDisbanded)
        {
            return ClanWarsErrors.ClanNotFound;
        }

        var receiptResult = ScoreReceipt.Create(change.SeasonId, change.ClanId, change.SourceId, change.Amount, change.Reason, change.ActorId, now);
        if (receiptResult.IsFailure)
        {
            return receiptResult.Error;
        }

        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        Season? season = await seasonRepository.GetForUpdateAsync(change.SeasonId, cancellationToken);
        if (season is null)
        {
            return ClanWarsErrors.SeasonNotFound;
        }

        if (!season.AcceptsScores)
        {
            return ClanWarsErrors.SeasonNotScoring;
        }

        await scoreRepository.AddReceiptAsync(receiptResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error.IsUniqueViolation() ? ClanWarsErrors.ScoreAlreadyApplied : saveResult.Error;
        }

        await scoreRepository.ApplyAsync(change.SeasonId, change.ClanId, change.Amount, change.Battles, change.Wins, now, cancellationToken);

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        await UpdateCacheAsync(change.SeasonId, change.ClanId, cancellationToken);
        return receiptResult.Value;
    }

    private async Task UpdateCacheAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken)
    {
        try
        {
            SeasonScore? score = await scoreRepository.GetAsync(seasonId, clanId, cancellationToken);
            if (score is not null)
            {
                await leaderboardStore.SetAsync(seasonId, [score], cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to update leaderboard cache of season {SeasonId}", seasonId);
            syncSignal.Request(seasonId);
        }
    }
}
