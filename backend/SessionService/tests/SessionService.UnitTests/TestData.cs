using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;

namespace SessionService.UnitTests;

internal static class TestData
{
    public static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    public static GameServer Server(int capacity = 64) =>
        GameServer.Register(Guid.NewGuid(), "10.0.0.5:7777", "EU-West", ["free-roam", "ranked"], capacity, NOW).Value;

    public static Session FreeRoam(GameServer server, int maxPlayers = 2) =>
        Session.Create(server, SessionKind.FreeRoam, "free-roam", maxPlayers, matchId: null, NOW).Value;
}
