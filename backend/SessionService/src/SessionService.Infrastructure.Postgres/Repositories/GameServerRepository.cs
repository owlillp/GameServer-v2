using Microsoft.EntityFrameworkCore;
using SessionService.Core.Abstractions;
using SessionService.Domain.Servers;

namespace SessionService.Infrastructure.Postgres.Repositories;

public sealed class GameServerRepository(SessionServiceDbContext dbContext) : IGameServerRepository
{
    private const int MAX_CANDIDATES = 200;

    public Task<GameServer?> GetAsync(Guid serverId, CancellationToken cancellationToken) =>
        dbContext.GameServers.FirstOrDefaultAsync(server => server.Id == serverId, cancellationToken);

    public async Task<IReadOnlyList<GameServer>> ListOnlineAsync(
        string region,
        DateTime heartbeatAfter,
        CancellationToken cancellationToken) =>
        await dbContext.GameServers
            .AsNoTracking()
            .Where(server => server.Region == region
                && server.Status == GameServerStatus.Online
                && server.LastHeartbeatAt >= heartbeatAfter)
            .OrderByDescending(server => server.LastHeartbeatAt)
            .Take(MAX_CANDIDATES)
            .ToListAsync(cancellationToken);

    public Task AddAsync(GameServer server, CancellationToken cancellationToken) =>
        dbContext.GameServers.AddAsync(server, cancellationToken).AsTask();
}
