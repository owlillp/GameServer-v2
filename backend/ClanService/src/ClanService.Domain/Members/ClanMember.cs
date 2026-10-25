namespace ClanService.Domain.Members;

/// <summary>
/// The primary key is the account id: a player can be in one clan only.
/// The member count of the clan is kept by an atomic UPDATE, not by loading all members.
/// </summary>
public sealed class ClanMember
{
    public Guid AccountId { get; private set; }

    public Guid ClanId { get; private set; }

    public ClanRole Role { get; private set; }

    public DateTime JoinedAt { get; private set; }

    // EF Core
    private ClanMember() { }

    public ClanMember(Guid clanId, Guid accountId, ClanRole role, DateTime joinedAt)
    {
        ClanId = clanId;
        AccountId = accountId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public void ChangeRole(ClanRole role) => Role = role;
}
