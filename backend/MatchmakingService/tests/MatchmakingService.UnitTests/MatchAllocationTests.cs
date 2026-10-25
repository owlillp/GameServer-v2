using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;
using Xunit;

namespace MatchmakingService.UnitTests;

public sealed class MatchAllocationTests
{
    private static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkAllocated_RequiresRequest_AndIsIdempotentForSameSession()
    {
        Match match = CreateMatch();
        Guid session = Guid.NewGuid();

        Assert.True(match.MarkAllocated(session, "10.0.0.5:7777", NOW).IsFailure);

        match.RequestAllocation(NOW);
        Assert.True(match.IsWaitingForServer);

        Assert.True(match.MarkAllocated(session, "10.0.0.5:7777", NOW).IsSuccess);
        Assert.True(match.MarkAllocated(session, "10.0.0.5:7777", NOW).IsSuccess);
        Assert.True(match.MarkAllocated(Guid.NewGuid(), "10.0.0.6:7777", NOW).IsFailure);

        Assert.False(match.IsWaitingForServer);
        Assert.Equal(session, match.SessionId);
        Assert.Equal("10.0.0.5:7777", match.ServerAddress);
    }

    [Fact]
    public void MarkAllocated_RejectsCancelledMatch()
    {
        Match match = CreateMatch();
        match.RequestAllocation(NOW);
        match.Cancel(NOW);

        Assert.True(match.MarkAllocated(Guid.NewGuid(), "10.0.0.5:7777", NOW).IsFailure);
    }

    [Fact]
    public void Requeue_KeepsQueueTime_AndRaisesPriority()
    {
        Party party = QueuedParty();
        QueueTicket original = QueueTicket.Create(party, "ranked", "EU-West", NOW).Value;
        original.Match(Guid.NewGuid(), NOW.AddMinutes(1));
        party.MarkInMatch(NOW.AddMinutes(1));

        Assert.True(party.ReturnToQueue(NOW.AddMinutes(2)).IsSuccess);
        QueueTicket requeued = original.Requeue(party, NOW.AddMinutes(2)).Value;

        Assert.Equal(PartyStatus.Queued, party.Status);
        Assert.Equal(original.EnqueuedAt, requeued.EnqueuedAt);
        Assert.Equal("eu-west", requeued.Region);
        Assert.Equal("RANKED", requeued.Mode);
        Assert.True(requeued.IsPriority);
        Assert.Equal(1, requeued.Attempt);
        Assert.Equal(QueueTicketStatus.Queued, requeued.Status);
    }

    [Fact]
    public void Create_RejectsEmptyRegion()
    {
        Assert.True(QueueTicket.Create(QueuedParty(), "ranked", " ", NOW).IsFailure);
    }

    [Fact]
    public void ReturnToQueue_OnlyFromMatch()
    {
        Party party = QueuedParty();

        Assert.True(party.ReturnToQueue(NOW).IsFailure);
    }

    private static Party QueuedParty()
    {
        Guid leader = Guid.NewGuid();
        Guid mate = Guid.NewGuid();
        Party party = Party.Create(leader, NOW).Value;
        party.Join(mate, NOW);
        party.SetReady(leader, true, NOW);
        party.SetReady(mate, true, NOW);
        party.MarkQueued(leader, NOW);

        return party;
    }

    private static Match CreateMatch() =>
        Match.Create(
            QueuedParty(),
            QueuedParty(),
            "RANKED",
            NOW,
            NOW.AddHours(1),
            new MatchRewardData("coins", 100, 50, []),
            "eu-west").Value;
}
