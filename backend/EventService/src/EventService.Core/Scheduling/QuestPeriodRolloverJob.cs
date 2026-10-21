using EventService.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EventService.Core.Scheduling;

[DisallowConcurrentExecution]
public sealed class QuestPeriodRolloverJob(
    IServiceScopeFactory scopeFactory,
    ILogger<QuestPeriodRolloverJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var participationRepository = scope.ServiceProvider.GetRequiredService<IEventParticipationRepository>();

        DateTime now = DateTime.UtcNow;
        DateTime dailyPeriodStart = StartOfDay(now);
        DateTime weeklyPeriodStart = StartOfWeek(now);

        int deleted = await participationRepository.DeleteStaleQuestProgressAsync(
            dailyPeriodStart,
            weeklyPeriodStart,
            context.CancellationToken);

        logger.LogInformation(
            "Quest period rollover completed: removed {Count} stale progress rows",
            deleted);
    }

    private static DateTime StartOfDay(DateTime value) =>
        DateTime.SpecifyKind(value.ToUniversalTime().Date, DateTimeKind.Utc);

    private static DateTime StartOfWeek(DateTime value)
    {
        DateTime day = StartOfDay(value);
        int offset = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-offset);
    }
}
