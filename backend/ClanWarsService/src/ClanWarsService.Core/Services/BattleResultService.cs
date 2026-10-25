using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Seasons;
using ClanWarsService.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Turns a match result into a finished battle and season points, all in one transaction under
/// the season lock: the battle transition is a conditional UPDATE, each clan's points are a receipt
/// keyed by the battle. A redelivered result changes nothing; a result whose first delivery died
/// half-way is completed by the next one.
/// </summary>
public sealed class BattleResultService(
    IBattleRepository battleRepository,
    ISeasonRepository seasonRepository,
    ScoreService scoreService,
    IClanWarsEventPublisher eventPublisher,
    ITransactionManager transactionManager,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider,
    ILogger<BattleResultService> logger)
{
    public const string FINISHED = "finished";
    public const string CANCELLED = "cancelled";

    public async Task ApplyResultAsync(
        Guid battleId,
        Guid matchId,
        int? winnerSide,
        CancellationToken cancellationToken,
        long scoreA = -1,
        long scoreB = -1)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        Battle? battle = await battleRepository.GetBattleAsync(battleId, cancellationToken);
        if (battle is null || battle.Status == BattleStatus.Cancelled)
        {
            return;
        }

        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Season season = await seasonRepository.GetForUpdateAsync(battle.SeasonId, cancellationToken)
            ?? throw new InvalidOperationException($"Season {battle.SeasonId} of battle {battleId} was not found");

        if (!season.AcceptsScores)
        {
            // The result came after the season froze its standings.
            await CancelInTransactionAsync(battle, "season-closed", now, cancellationToken);
            (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();
            return;
        }

        ClanWarsOptions current = options.CurrentValue;
        var weakOpponent = new WeakOpponentPolicy(current.WeakOpponentGap, current.WeakOpponentScale, current.WeakOpponentMinFactor);
        BattleOutcome outcome = battle.Score(season.ScoringRules, winnerSide, weakOpponent);
        bool firstDelivery = await battleRepository.TryFinishAsync(battleId, matchId, outcome, now, cancellationToken);

        if (!firstDelivery && (await battleRepository.GetBattleAsync(battleId, cancellationToken))?.Status != BattleStatus.Finished)
        {
            // Cancelled concurrently (timeout, dead server): no points.
            return;
        }

        string reason = outcome.WinnerClanId is null ? "battle:draw" : "battle:result";

        await ApplyAsync(new ScoreChange(battle.SeasonId, battle.ClanAId, Source(battleId), outcome.PointsA, reason, null, 1, outcome.WinsA), battle.SquadA, cancellationToken);
        await ApplyAsync(new ScoreChange(battle.SeasonId, battle.ClanBId, Source(battleId), outcome.PointsB, reason, null, 1, outcome.WinsB), battle.SquadB, cancellationToken);

        if (firstDelivery)
        {
            await eventPublisher.PublishBattleFinishedAsync(battle, FINISHED, outcome, now);
            await DetectAbuseAsync(battle, winnerSide, scoreA, scoreB, current, now, cancellationToken);
        }

        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();

        await scoreService.RefreshCacheAsync(battle.SeasonId, [battle.ClanAId, battle.ClanBId], cancellationToken);

        logger.LogInformation(
            "Battle {BattleId} finished: winner {WinnerClanId}, {PointsA}:{PointsB}",
            battleId,
            outcome.WinnerClanId,
            outcome.PointsA,
            outcome.PointsB);
    }

    public async Task CancelAsync(Guid battleId, string reason, CancellationToken cancellationToken)
    {
        Battle? battle = await battleRepository.GetBattleAsync(battleId, cancellationToken);
        if (battle is null || !battle.IsOpen)
        {
            return;
        }

        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();
        await CancelInTransactionAsync(battle, reason, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();
    }

    /// <summary>
    /// Inside the caller's transaction; publishes battle-finished (cancelled) only if this call cancelled it.
    /// </summary>
    public async Task CancelInTransactionAsync(Battle battle, string reason, DateTime now, CancellationToken cancellationToken)
    {
        if (await battleRepository.TryCancelAsync(battle.Id, reason, now, cancellationToken))
        {
            await eventPublisher.PublishBattleFinishedAsync(battle, CANCELLED, null, now);
            logger.LogInformation("Battle {BattleId} cancelled: {Reason}", battle.Id, reason);
        }
    }

    private async Task DetectAbuseAsync(
        Battle battle,
        int? winnerSide,
        long scoreA,
        long scoreB,
        ClanWarsOptions current,
        DateTime now,
        CancellationToken cancellationToken)
    {
        int pairBattles = await battleRepository.CountFinishedBetweenAsync(battle.SeasonId, battle.ClanAId, battle.ClanBId, cancellationToken);

        IReadOnlyList<AbuseSignal> signals = AbuseDetector.Check(
            battle,
            winnerSide,
            scoreA,
            scoreB,
            pairBattles,
            new AbuseRules(current.WeakOpponentGap, current.RepeatedPairThreshold),
            now);

        if (signals.Count == 0)
        {
            return;
        }

        await battleRepository.AddSignalsAsync(signals, cancellationToken);

        foreach (AbuseSignal signal in signals)
        {
            await eventPublisher.PublishAbuseSignalAsync(signal);
            logger.LogWarning("Abuse signal {Kind} for battle {BattleId}: {Details}", signal.Kind, battle.Id, signal.Details);
        }

        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }

    private async Task ApplyAsync(ScoreChange change, IReadOnlyList<Guid> squad, CancellationToken cancellationToken)
    {
        var result = await scoreService.ApplyInTransactionAsync(change, squad, cancellationToken);
        if (result.IsFailure)
        {
            // A concurrent delivery won the receipt race: retry, the next attempt sees the receipt.
            throw result.Error == ClanWarsErrors.ScoreAlreadyApplied
                ? result.Error.AsTransient().ToException()
                : result.Error.ToHandlerException();
        }
    }

    private static string Source(Guid battleId) => $"battle:{battleId:N}";
}
