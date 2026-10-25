using ClanService.Core.Services;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Points;
using ClanService.Domain.Requests;
using Shared.SharedKernel.Errors;
using Xunit;

namespace ClanService.UnitTests;

public sealed class ClanDomainTests
{
    private static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("abc", "ABC")]
    [InlineData(" wolf1 ", "WOLF1")]
    public void Tag_IsNormalized(string input, string expected)
    {
        Assert.Equal(expected, Clan.NormalizeTag(input).Value);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("toolong")]
    [InlineData("w-lf")]
    [InlineData("волк")]
    public void Tag_RejectsInvalid(string input)
    {
        Assert.Equal(ClanErrors.TagInvalid, Clan.NormalizeTag(input).Error);
    }

    [Fact]
    public void Create_StartsWithLeaderOnly_AndFreeClanHasNoPendingFee()
    {
        Clan clan = Clan.Create(Guid.NewGuid(), "wolf", "Wolves", null, Guid.NewGuid(), feeHoldId: null, NOW).Value;

        Assert.Equal(1, clan.MemberCount);
        Assert.Equal(0, clan.ClanPoints);
        Assert.NotNull(clan.FeeCommittedAt);
        Assert.True(clan.IsActive);
    }

    [Fact]
    public void Create_WithFeeHold_WaitsForCommit()
    {
        Clan clan = Clan.Create(Guid.NewGuid(), "wolf", "Wolves", null, Guid.NewGuid(), Guid.NewGuid(), NOW).Value;

        Assert.Null(clan.FeeCommittedAt);
        clan.MarkFeeCommitted(NOW);
        Assert.Equal(NOW, clan.FeeCommittedAt);
    }

    [Fact]
    public void Disband_ResetsMembers()
    {
        Clan clan = Clan.Create(Guid.NewGuid(), "wolf", "Wolves", null, Guid.NewGuid(), null, NOW).Value;

        clan.Disband(NOW);

        Assert.Equal(ClanStatus.Disbanded, clan.Status);
        Assert.Equal(0, clan.MemberCount);
    }

    [Theory]
    [InlineData(ClanRole.Leader, ClanRole.Officer, true)]
    [InlineData(ClanRole.Leader, ClanRole.Member, true)]
    [InlineData(ClanRole.Officer, ClanRole.Member, true)]
    [InlineData(ClanRole.Officer, ClanRole.Officer, false)]
    [InlineData(ClanRole.Officer, ClanRole.Leader, false)]
    [InlineData(ClanRole.Member, ClanRole.Member, false)]
    public void Kick_FollowsHierarchy(ClanRole actor, ClanRole target, bool allowed)
    {
        Assert.Equal(allowed, ClanPolicy.CanKick(actor, target));
    }

    [Fact]
    public void ChangeRole_OnlyLeader_AndNeverToLeader()
    {
        Assert.True(ClanPolicy.CanChangeRole(ClanRole.Leader, ClanRole.Member, ClanRole.Officer));
        Assert.False(ClanPolicy.CanChangeRole(ClanRole.Officer, ClanRole.Member, ClanRole.Officer));
        Assert.False(ClanPolicy.CanChangeRole(ClanRole.Leader, ClanRole.Member, ClanRole.Leader));
        Assert.False(ClanPolicy.CanManageRequests(ClanRole.Member));
    }

    [Fact]
    public void Request_ResolvesOnce()
    {
        var request = new ClanJoinRequest(Guid.NewGuid(), Guid.NewGuid(), ClanRequestKind.Invite, Guid.NewGuid(), NOW);

        Assert.True(request.IsDecidedByPlayer);
        Assert.True(request.Accept(request.AccountId, NOW).IsSuccess);
        Assert.Equal(ClanErrors.RequestNotPending, request.Decline(request.AccountId, NOW).Error);
    }

    [Fact]
    public void History_OnlyVoluntaryLeaveStartsCooldown()
    {
        var member = new ClanMember(Guid.NewGuid(), Guid.NewGuid(), ClanRole.Member, NOW);

        Assert.True(new ClanMemberHistory(member, MemberLeaveReason.Left, NOW).StartsCooldown);
        Assert.False(new ClanMemberHistory(member, MemberLeaveReason.Kicked, NOW).StartsCooldown);
        Assert.False(new ClanMemberHistory(member, MemberLeaveReason.Disbanded, NOW).StartsCooldown);
    }

    [Fact]
    public void PointsEntry_RequiresAmountAndReason()
    {
        Assert.Equal(ClanErrors.PointsAmountInvalid, ClanPointsEntry.Create(Guid.NewGuid(), Guid.NewGuid(), 0, "x", null, null, NOW).Error);
        Assert.Equal(ClanErrors.PointsReasonInvalid, ClanPointsEntry.Create(Guid.NewGuid(), Guid.NewGuid(), 10, " ", null, null, NOW).Error);

        Guid operation = Guid.NewGuid();
        ClanPointsEntry entry = ClanPointsEntry.Create(Guid.NewGuid(), operation, -5, "penalty", Guid.NewGuid(), "abuse", NOW).Value;
        Assert.Equal(operation, entry.OperationId);
        Assert.Equal(-5, entry.Amount);
    }

    [Theory]
    [InlineData(ClanSaveErrors.TAG_INDEX, "clan.tag.taken")]
    [InlineData(ClanSaveErrors.PENDING_REQUEST_INDEX, "clan.request.duplicate")]
    [InlineData(ClanSaveErrors.MEMBER_KEY, "clan.member.already_in_clan")]
    public void UniqueViolation_MapsToBusinessError(string constraint, string expectedCode)
    {
        Error mapped = ClanSaveErrors.Map(GeneralErrors.UniqueConstraintViolation(constraint));

        Assert.Equal(expectedCode, mapped.Messages[0].Code);
    }
}
