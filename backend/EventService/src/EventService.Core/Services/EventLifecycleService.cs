using EventService.Core.Abstractions;
using EventService.Core.Configuration;
using EventService.Core.Messaging.Publishers;
using EventService.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;

namespace EventService.Core.Services;

public sealed class EventLifecycleService(
    IServiceScopeFactory scopeFactory,
    IOptions<EventsOptions> options,
    ILogger<EventLifecycleService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PhaseSweepIntervalSeconds));
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
                logger.LogError(exception, "Event phase sweep failed");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventsEventPublisher>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        DateTime now = DateTime.UtcNow;
        IReadOnlyList<GameEvent> events = await eventRepository.ListPendingNotificationsAsync(now, cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        foreach (GameEvent gameEvent in events)
        {
            EventPhase phase = gameEvent.GetPhase(now);

            if (gameEvent.StartedNotifiedAt is null
                && phase is EventPhase.ACTIVE or EventPhase.GRACE or EventPhase.CLOSED)
            {
                await eventPublisher.PublishStartedAsync(gameEvent, now, cancellationToken);
                gameEvent.MarkStartedNotified(now);

                logger.LogInformation("Event {EventCode} started", gameEvent.Code);
            }

            if (gameEvent.EndedNotifiedAt is null && phase == EventPhase.CLOSED)
            {
                await eventPublisher.PublishEndedAsync(gameEvent, now, cancellationToken);
                gameEvent.MarkEndedNotified(now);

                logger.LogInformation("Event {EventCode} ended", gameEvent.Code);
            }
        }

        await transactionManager.SaveChangesAsync(cancellationToken);
    }
}
