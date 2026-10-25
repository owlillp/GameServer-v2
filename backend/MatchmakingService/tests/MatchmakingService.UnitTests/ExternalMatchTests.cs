using MatchmakingService.Domain.Matches;
using Xunit;

namespace MatchmakingService.UnitTests;

public sealed class ExternalMatchTests
{
    private static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateExternal_BuildsTwoTeams_WithoutReward()
    {
        Guid a1 = Guid.NewGuid();
        Guid a2 = Guid.NewGuid();
        Guid b1 = Guid.NewGuid();

        Match match = Create(new Dictionary<int, IReadOnlyList<Guid>> { [1] = [a1, a2], [2] = [b1] });

        Assert.True(match.IsExternal);
        Assert.Null(match.Reward);
        Assert.Equal("CLAN-BATTLE", match.Mode);
        Assert.Equal([1, 2], match.Parties.Select(party => party.Side).Order());
        Assert.Equal(3, match.Participants.Count);
    }

    [Fact]
    public void Finish_WinnerIsTheTeamWithMoreScore()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        Match match = Create(new Dictionary<int, IReadOnlyList<Guid>> { [1] = [a], [2] = [b] });

        match.SubmitResults([new MatchResultData(a, 10, 1), new MatchResultData(b, 30, 3)], NOW);
        match.Finish(NOW);

        Assert.Equal(2, match.SideOf(match.WinnerPartyId!.Value));
    }

    [Fact]
    public void CreateExternal_RejectsSharedPlayersAndMissingTeam()
    {
        Guid shared = Guid.NewGuid();

        Assert.True(Match.CreateExternal("battle-1", "clan-battle", "eu-west",
            new Dictionary<int, IReadOnlyList<Guid>> { [1] = [shared], [2] = [shared] }, NOW, NOW.AddHours(1)).IsFailure);
        Assert.True(Match.CreateExternal("battle-1", "clan-battle", "eu-west",
            new Dictionary<int, IReadOnlyList<Guid>> { [1] = [shared] }, NOW, NOW.AddHours(1)).IsFailure);
        Assert.True(Match.CreateExternal(" ", "clan-battle", "eu-west",
            new Dictionary<int, IReadOnlyList<Guid>> { [1] = [Guid.NewGuid()], [2] = [Guid.NewGuid()] }, NOW, NOW.AddHours(1)).IsFailure);
    }

    private static Match Create(Dictionary<int, IReadOnlyList<Guid>> teams) =>
        Match.CreateExternal("battle-1", "clan-battle", "EU-West", teams, NOW, NOW.AddHours(1)).Value;
}
