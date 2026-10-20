using EventService.Domain.Events;
using EventService.Domain.Progress;

namespace EventService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<GameEvent> EventsRead { get; }

    IQueryable<EventProgress> ProgressRead { get; }

    IQueryable<EventQuestProgress> QuestProgressRead { get; }

    IQueryable<EventRewardClaim> ClaimsRead { get; }
}
