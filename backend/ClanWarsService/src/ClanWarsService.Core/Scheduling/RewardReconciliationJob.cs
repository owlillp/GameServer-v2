using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain.Rewards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Shared.Core.Database;
using WalletService.Contracts.Communication;

namespace ClanWarsService.Core.Scheduling;

/// <summary>
/// Daily check that delivered rewards really arrived. Currency: every credited grant of the last
/// days must have its operation in the Wallet ledger, a missing one is reopened and credited again
/// (idempotent). Items: commands not confirmed by Inventory are sent again (Inventory dedupes by the
/// grant id). Clan points go through the outbox to an idempotent ledger and need no check here.
/// Runs on one replica (clustered Quartz).
/// </summary>
[DisallowConcurrentExecution]
public sealed class RewardReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider,
    ILogger<RewardReconciliationJob> logger) : IJob
{
    private const int PAGE = 500;

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        ClanWarsOptions current = options.CurrentValue;

        int checkedCredits = 0;
        int reopenedCredits = 0;
        Guid? afterId = null;

        while (true)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var rewards = scope.ServiceProvider.GetRequiredService<IRewardRepository>();
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletCreditClient>();
            var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

            IReadOnlyList<SeasonRewardGrant> page = await rewards.ListCreditedSinceAsync(now.AddDays(-current.ReconciliationDays), afterId, PAGE, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            foreach (SeasonRewardGrant grant in page)
            {
                var exists = await wallet.OperationExistsAsync(grant.AccountId!.Value, grant.Id, cancellationToken);
                if (exists.IsSuccess && !exists.Value)
                {
                    grant.ReopenWallet(now);
                    reopenedCredits++;
                    logger.LogError("ALERT: reward grant {GrantId} is marked credited but Wallet has no such operation", grant.Id);
                }
            }

            (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
            checkedCredits += page.Count;
            afterId = page[^1].Id;
        }

        int resentItems = await ResendStuckItemsAsync(now, current, cancellationToken);

        logger.LogInformation(
            "Reward reconciliation: {Checked} credits checked, {Reopened} reopened, {Resent} item grants resent",
            checkedCredits,
            reopenedCredits,
            resentItems);
    }

    private async Task<int> ResendStuckItemsAsync(DateTime now, ClanWarsOptions current, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var rewards = scope.ServiceProvider.GetRequiredService<IRewardRepository>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        IReadOnlyList<SeasonRewardGrant> stuck = await rewards.ListStuckItemsAsync(now.AddMinutes(-current.StuckItemsMinutes), 10_000, cancellationToken);
        foreach (SeasonRewardGrant grant in stuck)
        {
            grant.ReopenItems(now);
        }

        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
        return stuck.Count;
    }
}
