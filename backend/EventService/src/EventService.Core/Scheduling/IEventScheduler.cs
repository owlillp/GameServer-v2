using EventService.Domain.Events;

namespace EventService.Core.Scheduling;

public interface IEventScheduler
{
    Task ReconcileAsync(CancellationToken cancellationToken);

    Task ScheduleAsync(GameEvent gameEvent, CancellationToken cancellationToken);

    Task UnscheduleAsync(Guid eventId, CancellationToken cancellationToken);
}
