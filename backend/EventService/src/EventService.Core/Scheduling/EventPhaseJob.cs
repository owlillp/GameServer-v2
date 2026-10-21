using EventService.Core.Abstractions;
using EventService.Core.Messaging.Publishers;
using EventService.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Core.Database;

namespace EventService.Core.Scheduling;

[DisallowConcurrentExecution]
public sealed class EventPhaseJob(
    IServiceScopeFactory scopeFactory,
    ILogger<EventPhaseJob> logger) : IJob
{
    public const string EVENT_ID_KEY = "eventId";
    public const string BOUNDARY_KEY = "boundary";

    public async Task Execute(IJobExecutionContext context)
    {
        string? rawEventId = context.MergedJobDataMap.GetString(EVENT_ID_KEY);
        string boundary = context.MergedJobDataMap.GetString(BOUNDARY_KEY) ?? "Unknown";

        if (!Guid.TryParse(rawEventId, out Guid eventId))
        {
            logger.LogError("Event phase job {JobKey} has an invalid event id", context.JobDetail.Key);
            return;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventsEventPublisher>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        var eventResult = await eventRepository.GetByIdAsync(
            eventId,
            includeDetails: false,
            context.CancellationToken);

        if (eventResult.IsFailure)
        {
            logger.LogWarning(
                "Event {EventId} for boundary {Boundary} was not found",
                eventId,
                boundary);

            return;
        }

        GameEvent gameEvent = eventResult.Value;
        if (gameEvent.Status != EventStatus.PUBLISHED)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        EventPhase phase = gameEvent.GetPhase(now);
        bool changed = false;

        if (gameEvent.StartedNotifiedAt is null
            && phase is EventPhase.ACTIVE or EventPhase.GRACE or EventPhase.CLOSED)
        {
            await eventPublisher.PublishStartedAsync(gameEvent, now, context.CancellationToken);
            gameEvent.MarkStartedNotified(now);
            changed = true;

            logger.LogInformation(
                "Published started event for {EventCode} at boundary {Boundary}",
                gameEvent.Code,
                boundary);
        }

        if (gameEvent.EndedNotifiedAt is null && phase == EventPhase.CLOSED)
        {
            await eventPublisher.PublishEndedAsync(gameEvent, now, context.CancellationToken);
            gameEvent.MarkEndedNotified(now);
            changed = true;

            logger.LogInformation(
                "Published ended event for {EventCode} at boundary {Boundary}",
                gameEvent.Code,
                boundary);
        }

        if (!changed)
        {
            logger.LogInformation(
                "Event {EventCode} reached boundary {Boundary}; phase is {Phase}",
                gameEvent.Code,
                boundary,
                phase);

            return;
        }

        var saveResult = await transactionManager.SaveChangesAsync(context.CancellationToken);
        if (saveResult.IsFailure)
        {
            string error = saveResult.Error.ToString();

            logger.LogError(
                "Failed to save phase notifications for event {EventCode}: {Error}",
                gameEvent.Code,
                error);

            throw new JobExecutionException(
                $"Failed to save phase notifications for event {gameEvent.Code}: {error}");
        }

        logger.LogInformation(
            "Event {EventCode} phase {Phase} persisted after boundary {Boundary}",
            gameEvent.Code,
            phase,
            boundary);
    }
}
