using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Seasons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using WalletService.Contracts.Communication;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Delivers season reward grants in batches claimed with <c>FOR UPDATE SKIP LOCKED</c> (replicas
/// share the work): clan points as an event to ClanService and items as a command to Inventory
/// (both through the outbox), currency as Wallet gRPC credits run with limited parallelism.
/// Every step is idempotent by the grant id; failures retry with exponential backoff and end in
/// Failed (alert) after the attempt limit. Seasons in Rewarding close when nothing is left.
/// </summary>
public sealed class SeasonRewardDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider,
    ILogger<SeasonRewardDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int delivered = await DispatchBatchAsync(stoppingToken);
                await CompleteSeasonsAsync(stoppingToken);

                if (delivered == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(options.CurrentValue.RewardIntervalSeconds), timeProvider, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Season reward dispatch failed");
                await Task.Delay(TimeSpan.FromSeconds(options.CurrentValue.RewardIntervalSeconds), timeProvider, stoppingToken);
            }
        }
    }

    internal async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var rewards = scope.ServiceProvider.GetRequiredService<IRewardRepository>();
        var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IClanWarsEventPublisher>();
        var wallet = scope.ServiceProvider.GetRequiredService<IWalletCreditClient>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        ClanWarsOptions current = options.CurrentValue;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        return await SkipLockedBatch.RunAsync(
            transactionManager,
            token => rewards.ClaimDueAsync(now, current.RewardBatchSize, token),
            async (grants, token) =>
            {
                Dictionary<Guid, int> seasonNumbers = await LoadSeasonNumbersAsync(seasons, grants, token);

                foreach (SeasonRewardGrant grant in grants)
                {
                    if (grant.NeedsPoints)
                    {
                        await publisher.PublishClanPointsGrantedAsync(grant, seasonNumbers.GetValueOrDefault(grant.SeasonId), now);
                        grant.MarkPointsPublished(now);
                    }

                    if (grant.NeedsItemsPublish)
                    {
                        await publisher.PublishGrantItemsAsync(grant, now);
                        grant.MarkItemsPublished(now);
                    }
                }

                await CreditAsync(wallet, grants.Where(grant => grant.NeedsWallet).ToList(), current, now, token);

                (await transactionManager.SaveChangesAsync(token)).ThrowIfFailed();
            },
            cancellationToken);
    }

    /// <summary>
    /// Wallet calls run in parallel; the grants are updated afterwards on this thread
    /// (the DbContext is not thread-safe).
    /// </summary>
    private async Task CreditAsync(
        IWalletCreditClient wallet,
        IReadOnlyList<SeasonRewardGrant> grants,
        ClanWarsOptions current,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (grants.Count == 0)
        {
            return;
        }

        var outcomes = new string?[grants.Count];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, grants.Count),
            new ParallelOptions { MaxDegreeOfParallelism = current.RewardParallelism, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                SeasonRewardGrant grant = grants[index];
                var result = await wallet.CreditAsync(
                    grant.AccountId!.Value,
                    grant.CurrencyCode!,
                    grant.CurrencyAmount,
                    grant.Id,
                    current.RewardWalletReason,
                    $"clanwars:season:{grant.SeasonId:N}",
                    token);

                outcomes[index] = result.IsSuccess ? null : result.Error.GetMessage();
            });

        TimeSpan backoff = TimeSpan.FromSeconds(current.RewardBackoffSeconds);

        for (int index = 0; index < grants.Count; index++)
        {
            SeasonRewardGrant grant = grants[index];

            if (outcomes[index] is null)
            {
                grant.MarkWalletCredited(now);
                continue;
            }

            grant.RecordFailure(outcomes[index]!, now, backoff, current.RewardMaxAttempts);

            if (grant.Status == RewardGrantStatus.Failed)
            {
                logger.LogError(
                    "ALERT: reward grant {GrantId} for account {AccountId} failed after {Attempts} attempts: {Error}",
                    grant.Id,
                    grant.AccountId,
                    grant.Attempts,
                    grant.LastError);
            }
        }
    }

    private async Task CompleteSeasonsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();

        foreach (Season season in await seasons.ListByStatusAsync(SeasonStatus.Rewarding, cancellationToken))
        {
            await using AsyncServiceScope seasonScope = scopeFactory.CreateAsyncScope();
            await seasonScope.ServiceProvider.GetRequiredService<SeasonLifecycle>().CompleteRewardsAsync(season.Id, cancellationToken);
        }
    }

    private static async Task<Dictionary<Guid, int>> LoadSeasonNumbersAsync(
        ISeasonRepository seasons,
        IReadOnlyList<SeasonRewardGrant> grants,
        CancellationToken cancellationToken)
    {
        var numbers = new Dictionary<Guid, int>();

        foreach (Guid seasonId in grants.Select(grant => grant.SeasonId).Distinct())
        {
            Season? season = await seasons.GetAsync(seasonId, cancellationToken);
            numbers[seasonId] = season?.Number ?? 0;
        }

        return numbers;
    }
}
