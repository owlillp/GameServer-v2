using ClanWarsService.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ClanWarsService.Core.Scheduling;

[DisallowConcurrentExecution]
public sealed class SeasonPhaseJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SeasonPhaseJob> logger) : IJob
{
    public const string SEASON_ID_KEY = "seasonId";

    public async Task Execute(IJobExecutionContext context)
    {
        if (!Guid.TryParse(context.MergedJobDataMap.GetString(SEASON_ID_KEY), out Guid seasonId))
        {
            logger.LogError("Season phase job {JobKey} has an invalid season id", context.JobDetail.Key);
            return;
        }

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var lifecycle = scope.ServiceProvider.GetRequiredService<SeasonLifecycle>();

            await lifecycle.AdvanceAsync(seasonId, context.CancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Refire: the transition is idempotent, the trigger must not be lost.
            throw new JobExecutionException(exception, refireImmediately: false);
        }
    }
}
