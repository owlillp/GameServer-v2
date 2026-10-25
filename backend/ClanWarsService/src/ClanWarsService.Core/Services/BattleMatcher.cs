using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Seasons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Pairs queued squads while a battle window is open, and cancels battles that never reported.
/// Replicas share the queue through <c>FOR UPDATE SKIP LOCKED</c>. A pair needs the same squad size,
/// a rating gap within a tolerance that grows with waiting, and fewer than the daily limit of
/// battles between the two clans. The battle and the match command are written in one transaction.
/// </summary>
public sealed class BattleMatcher(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider,
    ILogger<BattleMatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.CurrentValue.MatcherIntervalSeconds), timeProvider);

        while (await WaitAsync(timer, stoppingToken))
        {
            try
            {
                await PairAsync(stoppingToken);
                await CancelStaleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Battle matcher sweep failed");
            }
        }
    }

    internal async Task<int> PairAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
        var battles = scope.ServiceProvider.GetRequiredService<IBattleRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IClanWarsEventPublisher>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Season? season = await seasons.GetCurrentAsync(cancellationToken);

        if (season is null
            || season.Status != SeasonStatus.Active
            || !await battles.IsWindowOpenAsync(season.Id, now, cancellationToken))
        {
            return 0;
        }

        ClanWarsOptions current = options.CurrentValue;
        var rules = new PairingRules(current.BaseRatingTolerance, current.RatingTolerancePerMinute);
        int created = 0;

        await SkipLockedBatch.RunAsync(
            transactionManager,
            token => battles.ClaimQueuedAsync(season.Id, current.MatcherBatchSize, token),
            async (queued, token) =>
            {
                var waiting = queued.ToList();

                while (waiting.Count > 1)
                {
                    BattleQueueEntry first = waiting[0];
                    waiting.RemoveAt(0);

                    BattleQueueEntry? opponent = await FindOpponentAsync(first, waiting, battles, rules, current, now, token);
                    if (opponent is null)
                    {
                        continue;
                    }

                    waiting.Remove(opponent);

                    var battle = Battle.Create(season.Id, first, opponent, now);
                    first.MarkMatched(battle.Id, now);
                    opponent.MarkMatched(battle.Id, now);

                    await battles.AddBattleAsync(battle, token);
                    await publisher.PublishBattleCreatedAsync(battle, Battle.MODE, current.BattleRegion);
                    created++;

                    logger.LogInformation(
                        "Battle {BattleId}: clan {ClanA} vs clan {ClanB}",
                        battle.Id,
                        battle.ClanAId,
                        battle.ClanBId);
                }

                (await transactionManager.SaveChangesAsync(token)).ThrowIfFailed();
            },
            cancellationToken);

        return created;
    }

    private static async Task<BattleQueueEntry?> FindOpponentAsync(
        BattleQueueEntry entry,
        IReadOnlyList<BattleQueueEntry> candidates,
        IBattleRepository battles,
        PairingRules rules,
        ClanWarsOptions current,
        DateTime now,
        CancellationToken cancellationToken)
    {
        DateTime dayStart = now.Date;

        foreach (BattleQueueEntry candidate in candidates
            .Where(candidate => BattlePairing.CanPair(entry, candidate, rules, now))
            .OrderBy(candidate => Math.Abs(candidate.Rating - entry.Rating))
            .ThenBy(candidate => candidate.QueuedAt))
        {
            int today = await battles.CountBattlesBetweenSinceAsync(entry.ClanId, candidate.ClanId, dayStart, cancellationToken);
            if (today < current.MaxBattlesPerPairPerDay)
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task CancelStaleAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var battles = scope.ServiceProvider.GetRequiredService<IBattleRepository>();

        DateTime createdBefore = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-options.CurrentValue.BattleTimeoutMinutes);
        IReadOnlyList<Guid> stale = await battles.ListOpenAsync(null, createdBefore, options.CurrentValue.MatcherBatchSize, cancellationToken);

        foreach (Guid battleId in stale)
        {
            await using AsyncServiceScope battleScope = scopeFactory.CreateAsyncScope();
            await battleScope.ServiceProvider.GetRequiredService<BattleResultService>().CancelAsync(battleId, "timeout", cancellationToken);
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
