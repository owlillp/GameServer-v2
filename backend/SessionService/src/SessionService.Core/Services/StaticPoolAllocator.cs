using Microsoft.Extensions.Options;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Domain.Servers;

namespace SessionService.Core.Services;

/// <summary>
/// Dev allocator over servers that are already running and registered themselves.
/// Picks the live server of the region with the most free player slots.
/// A Fleet API allocator will replace it behind <see cref="IGameServerAllocator"/>.
/// </summary>
public sealed class StaticPoolAllocator(
    IGameServerRepository serverRepository,
    ISessionRepository sessionRepository,
    ISessionRegistry registry,
    IOptionsMonitor<SessionOptions> options,
    TimeProvider timeProvider) : IGameServerAllocator
{
    public async Task<GameServer?> AllocateAsync(
        string region,
        string mode,
        int players,
        CancellationToken cancellationToken)
    {
        SessionOptions current = options.CurrentValue;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime heartbeatAfter = now.AddSeconds(-(current.HeartbeatPersistSeconds + current.ServerTtlSeconds));

        IReadOnlyList<GameServer> candidates = [.. (await serverRepository.ListOnlineAsync(
                region,
                heartbeatAfter,
                cancellationToken))
            .Where(server => server.Supports(mode) && server.Capacity >= players)];

        if (candidates.Count == 0)
        {
            return null;
        }

        Guid[] ids = [.. candidates.Select(server => server.Id)];
        IReadOnlySet<Guid> alive = await registry.GetAliveServersAsync(ids, cancellationToken);
        IReadOnlyDictionary<Guid, int> reserved = await sessionRepository.SumReservedSlotsByServerAsync(ids, cancellationToken);

        return candidates
            .Where(server => alive.Contains(server.Id))
            .Select(server => (Server: server, Free: server.Capacity - reserved.GetValueOrDefault(server.Id)))
            .Where(candidate => candidate.Free >= players)
            .OrderByDescending(candidate => candidate.Free)
            .Select(candidate => candidate.Server)
            .FirstOrDefault();
    }
}
