namespace SessionService.Core.Abstractions;

/// <summary>
/// Live registry in Redis: keys expire unless game server heartbeats extend them.
/// </summary>
public interface ISessionRegistry
{
    Task TouchServerAsync(Guid serverId, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the servers whose presence key still exists.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetAliveServersAsync(IReadOnlyCollection<Guid> serverIds, CancellationToken cancellationToken);

    Task TouchSessionsAsync(IReadOnlyCollection<Guid> sessionIds, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the sessions whose live key still exists.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetAliveAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken);

    Task RemoveSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a ticket id as used; false if it was used before (replay).
    /// </summary>
    Task<bool> TryUseTicketAsync(string ticketId, TimeSpan ttl, CancellationToken cancellationToken);
}
