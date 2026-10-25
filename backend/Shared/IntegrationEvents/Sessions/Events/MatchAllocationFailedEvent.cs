namespace Shared.IntegrationEvents.Sessions.Events;

/// <param name="Reason">Machine readable reason, e.g. "no-server".</param>
public sealed record MatchAllocationFailedEvent(
    Guid MatchId,
    string Reason,
    DateTime FailedAt);
