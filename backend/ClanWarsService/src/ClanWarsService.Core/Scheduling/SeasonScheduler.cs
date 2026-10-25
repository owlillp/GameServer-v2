using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain.Seasons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl.Matchers;

namespace ClanWarsService.Core.Scheduling;

/// <summary>
/// One durable Quartz job per season with a trigger at each boundary (start, end, finalization).
/// Triggers live in PostgreSQL (clustered JobStore); a boundary missed while the service was down
/// fires as soon as it is back (<c>misfire = FireNow</c>), and a boundary already in the past
/// when scheduling fires immediately.
/// </summary>
public sealed class SeasonScheduler(
    ISchedulerFactory schedulerFactory,
    ISeasonRepository seasonRepository,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider,
    ILogger<SeasonScheduler> logger) : ISeasonScheduler
{
    private const string JOB_GROUP = "seasons";
    private const string JOB_PREFIX = "season-";
    private const string SYSTEM_GROUP = "system";

    private static readonly JobKey RECONCILIATION_JOB = new("reward-reconciliation", SYSTEM_GROUP);
    private static readonly TriggerKey RECONCILIATION_TRIGGER = new("reward-reconciliation-daily", SYSTEM_GROUP);

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        IReadOnlyList<Season> open = await seasonRepository.ListOpenAsync(cancellationToken);
        HashSet<Guid> openIds = [.. open.Select(season => season.Id)];

        foreach (JobKey key in await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(JOB_GROUP), cancellationToken))
        {
            if (!TryGetSeasonId(key.Name, out Guid seasonId) || !openIds.Contains(seasonId))
            {
                await scheduler.DeleteJob(key, cancellationToken);
            }
        }

        foreach (Season season in open)
        {
            await ScheduleAsync(season, cancellationToken);
        }

        await EnsureReconciliationAsync(scheduler, cancellationToken);

        logger.LogInformation("Season scheduler reconciled {Count} open seasons", open.Count);
    }

    public async Task ScheduleAsync(Season season, CancellationToken cancellationToken)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        JobKey jobKey = new($"{JOB_PREFIX}{season.Id:N}", JOB_GROUP);

        if (await scheduler.CheckExists(jobKey, cancellationToken))
        {
            await scheduler.DeleteJob(jobKey, cancellationToken);
        }

        if (season.Status is SeasonStatus.Draft or SeasonStatus.Closed)
        {
            return;
        }

        IJobDetail job = JobBuilder.Create<SeasonPhaseJob>()
            .WithIdentity(jobKey)
            .UsingJobData(SeasonPhaseJob.SEASON_ID_KEY, season.Id.ToString())
            .StoreDurably()
            .Build();

        await scheduler.AddJob(job, replace: true, cancellationToken);

        DateTime earliest = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(1);
        (string Name, DateTime At)[] boundaries =
        [
            ("start", season.StartsAt),
            ("end", season.EndsAt),
            ("finalize", season.FinalizesAt),
        ];

        foreach ((string name, DateTime at) in boundaries)
        {
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity($"{jobKey.Name}-{name}", JOB_GROUP)
                .ForJob(job)
                .StartAt(new DateTimeOffset(DateTime.SpecifyKind(at > earliest ? at : earliest, DateTimeKind.Utc)))
                .WithSimpleSchedule(schedule => schedule
                    .WithRepeatCount(0)
                    .WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(trigger, cancellationToken);
        }

        logger.LogInformation("Scheduled phase triggers for season {Number}", season.Number);
    }

    private async Task EnsureReconciliationAsync(IScheduler scheduler, CancellationToken cancellationToken)
    {
        IJobDetail job = JobBuilder.Create<RewardReconciliationJob>()
            .WithIdentity(RECONCILIATION_JOB)
            .StoreDurably()
            .Build();

        await scheduler.AddJob(job, replace: true, cancellationToken);

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity(RECONCILIATION_TRIGGER)
            .ForJob(RECONCILIATION_JOB)
            .WithCronSchedule(
                options.CurrentValue.ReconciliationCron,
                schedule => schedule.InTimeZone(TimeZoneInfo.Utc).WithMisfireHandlingInstructionFireAndProceed())
            .Build();

        if (await scheduler.CheckExists(RECONCILIATION_TRIGGER, cancellationToken))
        {
            await scheduler.RescheduleJob(RECONCILIATION_TRIGGER, trigger, cancellationToken);
        }
        else
        {
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }
    }

    private static bool TryGetSeasonId(string jobName, out Guid seasonId)
    {
        seasonId = Guid.Empty;
        return jobName.StartsWith(JOB_PREFIX, StringComparison.Ordinal)
            && Guid.TryParseExact(jobName[JOB_PREFIX.Length..], "N", out seasonId);
    }
}
