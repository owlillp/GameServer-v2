namespace EventService.Domain.Events;

public enum EventKind
{
    SEASONAL,
    HOLIDAY,
    SPECIAL,
}

public enum EventStatus
{
    DRAFT,
    PUBLISHED,
    ARCHIVED,
}

public enum EventPhase
{
    DRAFT,
    SCHEDULED,
    ANNOUNCED,
    ACTIVE,
    GRACE,
    CLOSED,
}

public enum EventQuestKind
{
    DAILY,
    WEEKLY,
    SEASONAL,
}

public enum EventQuestMetric
{
    MATCHES,
    WINS,
    KILLS,
    SCORE,
}
