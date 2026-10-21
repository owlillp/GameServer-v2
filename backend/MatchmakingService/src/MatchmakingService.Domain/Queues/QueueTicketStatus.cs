namespace MatchmakingService.Domain.Queues;

public enum QueueTicketStatus
{
    Queued = 1,
    Matched = 2,
    Cancelled = 3,
}
