using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;

namespace MatchmakingService.Core.Services;

/// <summary>
/// Background pairing, expiry and reward retry. Safe on several replicas: every step claims its
/// rows with <c>FOR UPDATE SKIP LOCKED</c> inside a transaction, so rows taken by one replica
/// are skipped by the others.
/// </summary>
public sealed class MatchmakingCoordinator(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<MatchmakingOptions> options,
    TimeProvider timeProvider,
    ILogger<MatchmakingCoordinator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeSpan interval = TimeSpan.FromSeconds(options.CurrentValue.PairingIntervalSeconds);
                await Task.Delay(interval, timeProvider, stoppingToken);

                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Matchmaking sweep failed");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await RunStepAsync("pairing", PairQueuedPartiesAsync, cancellationToken);
        await RunStepAsync("expired matches", FinishExpiredMatchesAsync, cancellationToken);
        await RunStepAsync("allocation timeouts", CancelUnallocatedMatchesAsync, cancellationToken);
        await RunStepAsync("reward retry", RetryPendingRewardsAsync, cancellationToken);
    }

    private async Task RunStepAsync(
        string name,
        Func<CancellationToken, Task> step,
        CancellationToken cancellationToken)
    {
        try
        {
            await step(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Matchmaking step {Step} failed", name);
        }
    }

    private async Task PairQueuedPartiesAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var queueRepository = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        MatchmakingOptions current = options.CurrentValue;
        var created = new List<Match>();

        await SkipLockedBatch.RunAsync(
            transactionManager,
            token => queueRepository.ClaimQueuedAsync(current.PairingBatchSize, token),
            async (tickets, token) =>
            {
                created.AddRange(await PairAsync(scope.ServiceProvider, tickets, current, token));

                var saveResult = await transactionManager.SaveChangesAsync(token);
                saveResult.ThrowIfFailed();
            },
            cancellationToken);

        foreach (Match match in created)
        {
            logger.LogInformation(
                "Paired parties {FirstPartyId} and {SecondPartyId} into match {MatchId}",
                match.Parties[0].PartyId,
                match.Parties[1].PartyId,
                match.Id);
        }
    }

    private async Task<List<Match>> PairAsync(
        IServiceProvider services,
        IReadOnlyList<QueueTicket> tickets,
        MatchmakingOptions current,
        CancellationToken cancellationToken)
    {
        var partyRepository = services.GetRequiredService<IPartyRepository>();
        var matchRepository = services.GetRequiredService<IMatchRepository>();
        var eventPublisher = services.GetRequiredService<IMatchmakingEventPublisher>();
        var created = new List<Match>();

        IEnumerable<IGrouping<string, QueueTicket>> groups = current.RequireSamePartySize
            ? tickets.GroupBy(ticket => $"{ticket.Mode}:{ticket.Region}:{ticket.PartySize}", StringComparer.Ordinal)
            : tickets.GroupBy(ticket => $"{ticket.Mode}:{ticket.Region}", StringComparer.Ordinal);

        foreach (IGrouping<string, QueueTicket> group in groups)
        {
            List<QueueTicket> ordered = [.. group
                .OrderByDescending(ticket => ticket.IsPriority)
                .ThenBy(ticket => ticket.EnqueuedAt)];

            for (int index = 0; index + 1 < ordered.Count; index += 2)
            {
                QueueTicket first = ordered[index];
                QueueTicket second = ordered[index + 1];

                IReadOnlyList<Party> parties = await partyRepository.ListAsync(
                    [first.PartyId, second.PartyId],
                    cancellationToken);

                if (parties.Count != 2 || parties.Any(party => party.Status != PartyStatus.Queued))
                {
                    continue;
                }

                Match? match = TryCreateMatch(first, second, parties, current);
                if (match is null)
                {
                    continue;
                }

                await matchRepository.AddAsync(match, cancellationToken);

                if (current.ResultsSource == ResultsSource.Session)
                {
                    // Sent through the outbox together with the commit of the new match.
                    match.RequestAllocation(timeProvider.GetUtcNow().UtcDateTime);
                    await eventPublisher.PublishAllocateMatchAsync(match, cancellationToken);
                }

                created.Add(match);
            }
        }

        return created;
    }

    private Match? TryCreateMatch(
        QueueTicket first,
        QueueTicket second,
        IReadOnlyList<Party> parties,
        MatchmakingOptions current)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        var reward = new MatchRewardData(
            current.DefaultCurrencyCode,
            current.DefaultParticipationReward,
            current.DefaultWinBonus,
            []);

        var matchResult = Match.Create(
            parties[0],
            parties[1],
            first.Mode,
            now,
            now.AddHours(current.DefaultMatchDurationHours),
            reward,
            first.Region);

        if (matchResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to create match for parties {FirstPartyId} and {SecondPartyId}: {Error}",
                first.PartyId,
                second.PartyId,
                matchResult.Error.ToString());

            return null;
        }

        Match match = matchResult.Value;

        if (parties[0].MarkInMatch(now).IsFailure || parties[1].MarkInMatch(now).IsFailure)
        {
            return null;
        }

        first.Match(match.Id, now);
        second.Match(match.Id, now);

        return match;
    }

    private async Task FinishExpiredMatchesAsync(CancellationToken cancellationToken)
    {
        MatchmakingOptions current = options.CurrentValue;
        if (!current.AutoFinishMatches)
        {
            return;
        }

        for (int processed = 0; processed < current.ExpiredMatchBatchSize; processed++)
        {
            if (!await FinishNextExpiredMatchAsync(cancellationToken))
            {
                return;
            }
        }
    }

    /// <summary>
    /// One match per transaction (and per scope), so a failing match does not block the others.
    /// </summary>
    private async Task<bool> FinishNextExpiredMatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var partyRepository = scope.ServiceProvider.GetRequiredService<IPartyRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var finalizer = scope.ServiceProvider.GetRequiredService<MatchFinalizer>();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Guid? matchId = await matchRepository.ClaimExpiredActiveAsync(now, cancellationToken);
        if (matchId is null)
        {
            return false;
        }

        Match match = await matchRepository.GetAsync(matchId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Claimed match {matchId} was not found");

        MatchFinalization? finalization = null;

        if (match.HasResults)
        {
            var finishResult = await finalizer.FinishLockedAsync(match, cancellationToken);
            if (finishResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to finish expired match {MatchId}: {Error}",
                    match.Id,
                    finishResult.Error.ToString());

                return true;
            }

            finalization = finishResult.Value;
        }
        else if (match.IsExternal)
        {
            match.Cancel(now);
            await scope.ServiceProvider.GetRequiredService<IMatchmakingEventPublisher>()
                .PublishExternalCancelledAsync(match, "expired", cancellationToken);
        }
        else
        {
            match.Cancel(now);

            Guid[] partyIds = [.. match.Parties.Select(party => party.PartyId)];
            foreach (Party party in await partyRepository.ListAsync(partyIds, cancellationToken))
            {
                party.ReleaseFromMatch(now);
            }
        }

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            logger.LogError(
                "Failed to close expired match {MatchId}: {Error}",
                match.Id,
                commitResult.Error.ToString());

            return true;
        }

        if (finalization is null)
        {
            logger.LogInformation("Cancelled expired match {MatchId} without results", match.Id);
        }
        else
        {
            await finalizer.CompleteAsync(finalization, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Matches that got no game server in time are cancelled and their parties re-queued.
    /// </summary>
    private async Task CancelUnallocatedMatchesAsync(CancellationToken cancellationToken)
    {
        MatchmakingOptions current = options.CurrentValue;

        for (int processed = 0; processed < current.ExpiredMatchBatchSize; processed++)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
            var recovery = scope.ServiceProvider.GetRequiredService<MatchRecovery>();

            DateTime now = timeProvider.GetUtcNow().UtcDateTime;

            (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

            Guid? matchId = await matchRepository.ClaimAllocationTimedOutAsync(
                now.AddSeconds(-current.AllocationTimeoutSeconds),
                cancellationToken);

            if (matchId is null)
            {
                return;
            }

            Match match = await matchRepository.GetAsync(matchId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Claimed match {matchId} was not found");

            await recovery.CancelAndRequeueAsync(match, "allocation-timeout", now, cancellationToken);

            (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();
        }
    }

    private async Task RetryPendingRewardsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<MatchRewardDispatcher>();

        await dispatcher.DispatchPendingAsync(grantIds: null, cancellationToken);
    }
}
