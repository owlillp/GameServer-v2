namespace RealtimeService.Core.Abstractions;

/// <summary>
/// Projection of clan membership built from ClanService events, plus the live SignalR
/// connections of each player, so clan groups can be joined on connect and on join.
/// </summary>
public interface IClanRosterStore
{
    Task SetClanAsync(Guid accountId, Guid clanId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the membership only if it still points at <paramref name="clanId"/>
    /// (a late "left" must not erase a newer clan).
    /// </summary>
    Task RemoveClanAsync(Guid accountId, Guid clanId, CancellationToken cancellationToken);

    Task<Guid?> GetClanAsync(Guid accountId, CancellationToken cancellationToken);

    Task AddConnectionAsync(Guid accountId, string connectionId, CancellationToken cancellationToken);

    Task RemoveConnectionAsync(Guid accountId, string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetConnectionsAsync(Guid accountId, CancellationToken cancellationToken);
}
