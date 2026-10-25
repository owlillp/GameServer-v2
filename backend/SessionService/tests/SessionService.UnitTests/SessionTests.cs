using SessionService.Domain;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;
using Xunit;

namespace SessionService.UnitTests;

public sealed class SessionTests
{
    [Fact]
    public void Register_NormalizesRegionAndModes()
    {
        GameServer server = TestData.Server();

        Assert.Equal("eu-west", server.Region);
        Assert.Equal(["FREE-ROAM", "RANKED"], server.Modes);
        Assert.True(server.Supports("ranked"));
    }

    [Fact]
    public void Create_RejectsUnsupportedMode()
    {
        GameServer server = TestData.Server();

        var result = Session.Create(server, SessionKind.FreeRoam, "battle-royale", 10, null, TestData.NOW);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_MatchSessionRequiresMatchId()
    {
        GameServer server = TestData.Server();

        Assert.True(Session.Create(server, SessionKind.Match, "ranked", 10, null, TestData.NOW).IsFailure);
        Assert.True(Session.Create(server, SessionKind.Match, "ranked", 10, Guid.NewGuid(), TestData.NOW).IsSuccess);
    }

    [Fact]
    public void Join_EnforcesCapacity_AndIsIdempotent()
    {
        Session session = TestData.FreeRoam(TestData.Server(), maxPlayers: 2);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Assert.True(session.Join(first, TestData.NOW).IsSuccess);
        Assert.True(session.Join(first, TestData.NOW).IsSuccess);
        Assert.True(session.Join(second, TestData.NOW).IsSuccess);

        var third = session.Join(Guid.NewGuid(), TestData.NOW);

        Assert.True(third.IsFailure);
        Assert.Equal(SessionErrors.SessionFull, third.Error);
        Assert.Equal(2, session.PlayerCount);
    }

    [Fact]
    public void Leave_FreesSlot_AndPlayerCanRejoin()
    {
        Session session = TestData.FreeRoam(TestData.Server(), maxPlayers: 1);
        Guid player = Guid.NewGuid();

        session.Join(player, TestData.NOW);
        Assert.True(session.Leave(player, TestData.NOW).IsSuccess);

        Assert.Equal(0, session.PlayerCount);
        Assert.False(session.IsPresent(player));
        Assert.True(session.HasPlayed(player));

        Assert.True(session.Join(player, TestData.NOW.AddMinutes(1)).IsSuccess);
        Assert.Equal(1, session.PlayerCount);
    }

    [Fact]
    public void Close_ReturnsPresentPlayers_AndBlocksJoins()
    {
        Session session = TestData.FreeRoam(TestData.Server(), maxPlayers: 4);
        Guid stayed = Guid.NewGuid();
        Guid left = Guid.NewGuid();
        session.Join(stayed, TestData.NOW);
        session.Join(left, TestData.NOW);
        session.Leave(left, TestData.NOW);

        IReadOnlyList<Guid> present = session.Close("expired", TestData.NOW);

        Assert.Equal([stayed], present);
        Assert.Equal(SessionStatus.Closed, session.Status);
        Assert.Equal(0, session.PlayerCount);
        Assert.True(session.Join(Guid.NewGuid(), TestData.NOW).IsFailure);
        Assert.Empty(session.Close("expired", TestData.NOW));
    }
}
