namespace ClanService.Domain.Members;

public enum MemberLeaveReason
{
    Left = 1,
    Kicked = 2,
    Disbanded = 3,
}

/// <summary>
/// Past memberships: used for the re-join cooldown and for disputes.
/// </summary>
public sealed class ClanMemberHistory
{
    public Guid Id { get; private set; }

    public Guid ClanId { get; private set; }

    public Guid AccountId { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public DateTime LeftAt { get; private set; }

    public MemberLeaveReason Reason { get; private set; }

    // EF Core
    private ClanMemberHistory() { }

    public ClanMemberHistory(ClanMember member, MemberLeaveReason reason, DateTime leftAt)
    {
        Id = Guid.CreateVersion7();
        ClanId = member.ClanId;
        AccountId = member.AccountId;
        JoinedAt = member.JoinedAt;
        LeftAt = leftAt;
        Reason = reason;
    }

    /// <summary>
    /// Only leaving on one's own starts the cooldown; being kicked or disbanded does not.
    /// </summary>
    public bool StartsCooldown => Reason == MemberLeaveReason.Left;
}
