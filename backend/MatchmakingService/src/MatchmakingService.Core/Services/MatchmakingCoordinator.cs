using CSharpFunctionalExtensions;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Services;

public sealed class MatchmakingCoordinator(
    IServiceScopeFactory scopeFactory,
    IOptions<MatchmakingOptions> options,
    ILogger<MatchmakingCoordinator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PairingIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }

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
        await PairQueuedPartiesAsync(cancellationToken);
        await FinishExpiredMatchesAsync(cancellationToken);
        await RetryPendingRewardsAsync(cancellationToken);
    }

    private async Task PairQueuedPartiesAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var queueRepository = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var partyRepository = scope.ServiceProvider.GetRequiredService<IPartyRepository>();
        var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        IReadOnlyList<QueueTicket> tickets = await queueRepository.ListQueuedAsync(100, cancellationToken);
        if (tickets.Count < 2)
        {
            return;
        }

        MatchmakingOptions current = options.Value;

        IEnumerable<IGrouping<string, QueueTicket>> groups = current.RequireSamePartySize
            ? tickets.GroupBy(ticket => $"{ticket.Mode}:{ticket.PartySize}", StringComparer.Ordinal)
            : tickets.GroupBy(ticket => ticket.Mode, StringComparer.Ordinal);

        foreach (IGrouping<string, QueueTicket> group in groups)
        {
            List<QueueTicket> ordered = [.. group.OrderBy(ticket => ticket.EnqueuedAt)];

            for (int index = 0; index + 1 < ordered.Count; index += 2)
            {
                QueueTicket first = ordered[index];
                QueueTicket second = ordered[index + 1];

                var parties = await partyRepository.ListAsync(
                    [first.PartyId, second.PartyId],
                    cancellationToken);

                if (parties.Count != 2
                    || parties.Any(party => party.Status != PartyStatus.Queued))
                {
                    continue;
                }

                DateTime now = DateTime.UtcNow;
                DateTime endsAt = now.AddHours(current.DefaultMatchDurationHours);

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
                    endsAt,
                    reward);

                if (matchResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to create match for parties {FirstPartyId} and {SecondPartyId}: {Error}",
                        first.PartyId,
                        second.PartyId,
                        matchResult.Error.ToString());

                    continue;
                }

                Match match = matchResult.Value;

                UnitResult<Error> firstPartyResult = parties[0].MarkInMatch(now);
                UnitResult<Error> secondPartyResult = parties[1].MarkInMatch(now);

                if (firstPartyResult.IsFailure || secondPartyResult.IsFailure)
                {
                    continue;
                }

                first.Match(match.Id, now);
                second.Match(match.Id, now);

                await matchRepository.AddAsync(match, cancellationToken);

                var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
                if (saveResult.IsFailure)
                {
                    logger.LogError(
                        "Failed to save matched parties {FirstPartyId} and {SecondPartyId}: {Error}",
                        first.PartyId,
                        second.PartyId,
                        saveResult.Error.ToString());

                    continue;
                }

                logger.LogInformation(
                    "Paired parties {FirstPartyId} and {SecondPartyId} into match {MatchId}",
                    first.PartyId,
                    second.PartyId,
                    match.Id);
            }
        }
    }

    private async Task FinishExpiredMatchesAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.AutoFinishMatches)
        {
            return;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var partyRepository = scope.ServiceProvider.GetRequiredService<IPartyRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var finalizer = scope.ServiceProvider.GetRequiredService<MatchFinalizer>();

        IReadOnlyList<Match> expired = await matchRepository.ListExpiredActiveAsync(
            DateTime.UtcNow,
            20,
            cancellationToken);

        foreach (Match match in expired)
        {
            if (match.HasResults)
            {
                var finishResult = await finalizer.FinishAsync(match, cancellationToken);
                if (finishResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to finish expired match {MatchId}: {Error}",
                        match.Id,
                        finishResult.Error.ToString());
                }

                continue;
            }

            DateTime now = DateTime.UtcNow;
            match.Cancel(now);

            var partyIds = match.Parties.Select(party => party.PartyId).ToArray();
            IReadOnlyList<Party> parties = await partyRepository.ListAsync(partyIds, cancellationToken);

            foreach (Party party in parties)
            {
                party.ReleaseFromMatch(now);
            }

            var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError(
                    "Failed to cancel expired match {MatchId}: {Error}",
                    match.Id,
                    saveResult.Error.ToString());
                continue;
            }

            logger.LogInformation("Cancelled expired match {MatchId} without results", match.Id);
        }
    }

    private async Task RetryPendingRewardsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var rewardRepository = scope.ServiceProvider.GetRequiredService<IMatchRewardRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<MatchRewardDispatcher>();

        IReadOnlyList<MatchRewardGrant> pending = await rewardRepository.ListPendingAsync(
            50,
            cancellationToken);

        foreach (MatchRewardGrant grant in pending)
        {
            await dispatcher.DispatchAsync(grant, cancellationToken);
        }
    }
}
