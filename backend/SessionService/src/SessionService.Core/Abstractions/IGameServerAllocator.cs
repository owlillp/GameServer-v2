using SessionService.Domain.Servers;

namespace SessionService.Core.Abstractions;

/// <summary>
/// Picks (or, with a real fleet, starts) a game server for a new session.
/// </summary>
public interface IGameServerAllocator
{
    Task<GameServer?> AllocateAsync(string region, string mode, int players, CancellationToken cancellationToken);
}
