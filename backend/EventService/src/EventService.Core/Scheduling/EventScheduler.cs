using EventService.Core.Abstractions;
using EventService.Domain.Events;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace EventService.Core.Scheduling;

public sealed class EventScheduler(
    ISchedulerFactory schedulerFactory,
    IEventRepository eventRepository,
    ILogger<EventScheduler> logger) : IEventScheduler
{
    private const string EVENT_JOB_GROUP = "events";
    private const string SYSTEM_JOB_GROUP = "system";
    private const string QUEST_ROLLOVER_CRON = "0 0 0 * * ?";

    private static readonly JobKey QuestRolloverJobKey = new("quest-period-rollover", SYSTEM_JOB_GROUP);
    private static readonly TriggerKey QuestRolloverTriggerKey = new("quest-period-rollover-daily", SYSTEM_JOB_GROUP);

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        IReadOnlyList<GameEvent> events = await eventRepository.ListAsync(
            includeDetails: false,
            cancellationToken);

        var published = events
            .Where(gameEvent => gameEvent.Status == EventStatus.PUBLISHED)
            .ToList();

        var publishedIds = published
            .Select(gameEvent => gameEvent.Id)
            .ToHashSet();

        IReadOnlyCollection<JobKey> existingKeys = await scheduler.GetJobKeys(
            GroupMatcher<JobKey>.GroupEquals(EVENT_JOB_GROUP),
            cancellationToken);

        foreach (JobKey key in existingKeys)
        {
            if (TryGetEventId(key.Name, out Guid eventId) && !publishedIds.Contains(eventId))
            {
                await scheduler.DeleteJob(key, cancellationToken);
                logger.LogInformation("Removed stale event scheduler job {JobKey}", key);
            }
        }

        foreach (GameEvent gameEvent in published)
        {
            await ScheduleAsync(gameEvent, cancellationToken);
        }

        await EnsureQuestRolloverAsync(scheduler, cancellationToken);

        logger.LogInformation("Event scheduler reconciled {Count} published events", published.Count);
    }

    public async Task ScheduleAsync(GameEvent gameEvent, CancellationToken cancellationToken)
    {
        if (gameEvent.Status != EventStatus.PUBLISHED)
        {
            return;
        }

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        JobKey jobKey = EventJobKey(gameEvent.Id);

        if (await scheduler.CheckExists(jobKey, cancellationToken))
        {
            await scheduler.DeleteJob(jobKey, cancellationToken);
        }

        DateTime now = DateTime.UtcNow;
        DateTime earliestFire = now.AddSeconds(1);
        var triggers = new List<(EventBoundary Boundary, DateTime FireAt)>();

        if (gameEvent.AnnounceAt > now)
        {
            triggers.Add((EventBoundary.Announce, gameEvent.AnnounceAt));
        }

        if (gameEvent.StartedNotifiedAt is null)
        {
            triggers.Add((EventBoundary.Start, gameEvent.StartsAt > now ? gameEvent.StartsAt : earliestFire));
        }

        if (gameEvent.EndsAt > now)
        {
            triggers.Add((EventBoundary.Grace, gameEvent.EndsAt));
        }

        if (gameEvent.EndedNotifiedAt is null)
        {
            triggers.Add((EventBoundary.End, gameEvent.GraceEndsAt > now ? gameEvent.GraceEndsAt : earliestFire));
        }

        if (triggers.Count == 0)
        {
            return;
        }

        IJobDetail job = JobBuilder.Create<EventPhaseJob>()
            .WithIdentity(jobKey)
            .UsingJobData(EventPhaseJob.EVENT_ID_KEY, gameEvent.Id.ToString())
            .StoreDurably()
            .Build();

        await scheduler.AddJob(job, replace: true, cancellationToken);

        foreach ((EventBoundary boundary, DateTime fireAt) in triggers)
        {
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity($"{jobKey.Name}-{boundary.ToString().ToLowerInvariant()}", EVENT_JOB_GROUP)
                .ForJob(job)
                .UsingJobData(EventPhaseJob.BOUNDARY_KEY, boundary.ToString())
                .StartAt(new DateTimeOffset(DateTime.SpecifyKind(fireAt, DateTimeKind.Utc)))
                .WithSimpleSchedule(schedule => schedule
                    .WithRepeatCount(0)
                    .WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(trigger, cancellationToken);
        }

        logger.LogInformation(
            "Scheduled {Count} phase triggers for event {EventCode}",
            triggers.Count,
            gameEvent.Code);
    }

    public async Task UnscheduleAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.DeleteJob(EventJobKey(eventId), cancellationToken);
    }

    private static async Task EnsureQuestRolloverAsync(
        IScheduler scheduler,
        CancellationToken cancellationToken)
    {
        if (!await scheduler.CheckExists(QuestRolloverJobKey, cancellationToken))
        {
            IJobDetail job = JobBuilder.Create<QuestPeriodRolloverJob>()
                .WithIdentity(QuestRolloverJobKey)
                .StoreDurably()
                .Build();

            await scheduler.AddJob(job, replace: true, cancellationToken);
        }

        if (await scheduler.CheckExists(QuestRolloverTriggerKey, cancellationToken))
        {
            return;
        }

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity(QuestRolloverTriggerKey)
            .ForJob(QuestRolloverJobKey)
            .WithCronSchedule(
                QUEST_ROLLOVER_CRON,
                schedule => schedule
                    .InTimeZone(TimeZoneInfo.Utc)
                    .WithMisfireHandlingInstructionDoNothing())
            .Build();

        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    private static JobKey EventJobKey(Guid eventId) => new($"event-{eventId:N}", EVENT_JOB_GROUP);

    private static bool TryGetEventId(string jobName, out Guid eventId)
    {
        const string PREFIX = "event-";

        if (!jobName.StartsWith(PREFIX, StringComparison.Ordinal))
        {
            eventId = Guid.Empty;
            return false;
        }

        return Guid.TryParseExact(jobName[PREFIX.Length..], "N", out eventId);
    }
}
