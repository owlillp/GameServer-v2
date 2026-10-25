using SessionService.Domain.Servers;

namespace SessionService.Core.Abstractions;

public interface IGameServerRepository
{
    Task<GameServer?> GetAsync(Guid serverId, CancellationToken cancellationToken);

    /// <summary>
    /// Online servers of a region, freshest heartbeat first.
    /// </summary>
    Task<IReadOnlyList<GameServer>> ListOnlineAsync(
        string region,
        DateTime heartbeatAfter,
        CancellationToken cancellationToken);

    Task AddAsync(GameServer server, CancellationToken cancellationToken);
}
