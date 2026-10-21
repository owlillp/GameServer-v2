using Xunit;
using MatchmakingService.Domain.Parties;

namespace MatchmakingService.UnitTests;

public sealed class PartyTests
{
    private static readonly DateTime NOW = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_AddsLeaderAsMember()
    {
        Guid leaderId = Guid.NewGuid();

        Party party = Party.Create(leaderId, NOW).Value;

        Assert.Equal(leaderId, party.LeaderAccountId);
        Assert.Single(party.Members);
        Assert.True(party.IsMember(leaderId));
        Assert.Equal(PartyStatus.Forming, party.Status);
    }

    [Fact]
    public void Join_AddsMember_AndRejectsDuplicates()
    {
        Party party = Party.Create(Guid.NewGuid(), NOW).Value;
        Guid memberId = Guid.NewGuid();

        Assert.True(party.Join(memberId, NOW).IsSuccess);
        Assert.True(party.Join(memberId, NOW).IsFailure);
        Assert.Equal(2, party.Members.Count);
    }

    [Fact]
    public void MarkQueued_RequiresAllMembersReady()
    {
        Guid leaderId = Guid.NewGuid();
        Guid memberId = Guid.NewGuid();
        Party party = Party.Create(leaderId, NOW).Value;
        party.Join(memberId, NOW);

        Assert.True(party.MarkQueued(leaderId, NOW).IsFailure);

        party.SetReady(leaderId, true, NOW);
        party.SetReady(memberId, true, NOW);

        Assert.True(party.MarkQueued(leaderId, NOW).IsSuccess);
        Assert.Equal(PartyStatus.Queued, party.Status);
    }

    [Fact]
    public void Leave_PromotesOldestMemberToLeader()
    {
        Guid leaderId = Guid.NewGuid();
        Guid firstMember = Guid.NewGuid();
        Party party = Party.Create(leaderId, NOW).Value;
        party.Join(firstMember, NOW.AddMinutes(1));

        Assert.True(party.Leave(leaderId, NOW.AddMinutes(2)).IsSuccess);

        Assert.Equal(firstMember, party.LeaderAccountId);
        Assert.Single(party.Members);
    }

    [Fact]
    public void Join_RejectsMemberWhenPartyIsFull()
    {
        Party party = Party.Create(Guid.NewGuid(), NOW).Value;

        for (int index = 0; index < 4; index++)
        {
            Assert.True(party.Join(Guid.NewGuid(), NOW).IsSuccess);
        }

        Assert.True(party.Join(Guid.NewGuid(), NOW).IsFailure);
        Assert.Equal(5, party.Members.Count);
    }
}
