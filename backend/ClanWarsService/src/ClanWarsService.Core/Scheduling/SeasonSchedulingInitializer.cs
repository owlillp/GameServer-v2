using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClanWarsService.Core.Scheduling;

public sealed class SeasonSchedulingInitializer(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ClanWarsOptions> options,
    ILogger<SeasonSchedulingInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISeasonScheduler>().ReconcileAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                TimeSpan delay = TimeSpan.FromSeconds(options.CurrentValue.ReconcileRetrySeconds);
                logger.LogError(exception, "Failed to reconcile season scheduler, retrying in {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
