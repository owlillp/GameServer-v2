using EventService.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventService.Core.Scheduling;

public sealed class EventSchedulingInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<EventsOptions> options,
    ILogger<EventSchedulingInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan retryDelay = TimeSpan.FromSeconds(Math.Max(1, options.Value.ReconcileRetrySeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IEventScheduler>();

                await scheduler.ReconcileAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to reconcile event scheduler, retrying in {Delay}",
                    retryDelay);

                await Task.Delay(retryDelay, stoppingToken);
            }
        }
    }
}
