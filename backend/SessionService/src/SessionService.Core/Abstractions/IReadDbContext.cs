using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;

namespace SessionService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<GameServer> ServersRead { get; }

    IQueryable<Session> SessionsRead { get; }
}
