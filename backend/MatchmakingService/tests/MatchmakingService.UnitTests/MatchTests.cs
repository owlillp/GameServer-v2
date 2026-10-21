using Xunit;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Rewards;

namespace MatchmakingService.UnitTests;

public sealed class MatchTests
{
    private static readonly DateTime NOW = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Finish_SetsWinnerAndWinFlags()
    {
        (Party first, Party second, Guid firstPlayer, Guid secondPlayer) = CreateQueuedParties();
        Match match = CreateMatch(first, second);

        Assert.True(match.SubmitResults(
            [new MatchResultData(firstPlayer, 120, 7), new MatchResultData(secondPlayer, 80, 3)],
            NOW).IsSuccess);

        Assert.True(match.Finish(NOW).IsSuccess);

        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(first.Id, match.WinnerPartyId);
        Assert.True(match.Results.Single(result => result.AccountId == firstPlayer).IsWin);
        Assert.False(match.Results.Single(result => result.AccountId == secondPlayer).IsWin);
    }

    [Fact]
    public void Finish_TieHasNoWinner()
    {
        (Party first, Party second, Guid firstPlayer, Guid secondPlayer) = CreateQueuedParties();
        Match match = CreateMatch(first, second);

        match.SubmitResults(
            [new MatchResultData(firstPlayer, 50, 1), new MatchResultData(secondPlayer, 50, 1)],
            NOW);

        Assert.True(match.Finish(NOW).IsSuccess);

        Assert.Null(match.WinnerPartyId);
        Assert.All(match.Results, result => Assert.False(result.IsWin));
    }

    [Fact]
    public void SubmitResults_UpsertsExistingAccount()
    {
        (Party first, Party second, Guid firstPlayer, _) = CreateQueuedParties();
        Match match = CreateMatch(first, second);

        match.SubmitResults([new MatchResultData(firstPlayer, 10, 0)], NOW);
        match.SubmitResults([new MatchResultData(firstPlayer, 42, 4)], NOW.AddMinutes(5));

        MatchResult result = Assert.Single(match.Results);
        Assert.Equal(42, result.Score);
        Assert.Equal(4, result.Kills);
    }

    [Fact]
    public void SubmitResults_RejectsOutsider()
    {
        (Party first, Party second, _, _) = CreateQueuedParties();
        Match match = CreateMatch(first, second);

        var result = match.SubmitResults([new MatchResultData(Guid.NewGuid(), 10, 0)], NOW);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Finish_CannotBeRepeated()
    {
        (Party first, Party second, _, _) = CreateQueuedParties();
        Match match = CreateMatch(first, second);

        match.Finish(NOW);

        Assert.True(match.Finish(NOW.AddMinutes(1)).IsFailure);
    }

    private static (Party First, Party Second, Guid FirstPlayer, Guid SecondPlayer) CreateQueuedParties()
    {
        Guid firstPlayer = Guid.NewGuid();
        Guid firstMate = Guid.NewGuid();
        Guid secondPlayer = Guid.NewGuid();
        Guid secondMate = Guid.NewGuid();

        Party first = Party.Create(firstPlayer, NOW).Value;
        first.Join(firstMate, NOW);
        first.SetReady(firstPlayer, true, NOW);
        first.SetReady(firstMate, true, NOW);
        first.MarkQueued(firstPlayer, NOW);

        Party second = Party.Create(secondPlayer, NOW).Value;
        second.Join(secondMate, NOW);
        second.SetReady(secondPlayer, true, NOW);
        second.SetReady(secondMate, true, NOW);
        second.MarkQueued(secondPlayer, NOW);

        return (first, second, firstPlayer, secondPlayer);
    }

    private static Match CreateMatch(Party first, Party second) =>
        Match.Create(
            first,
            second,
            "RANKED",
            NOW,
            NOW.AddHours(48),
            new MatchRewardData("coins", 100, 50, [])).Value;
}
