namespace MatchmakingService.Domain.Parties;

public sealed class PartyMember
{
    public Guid PartyId { get; private set; }

    public Guid AccountId { get; private set; }

    public bool IsReady { get; private set; }

    public DateTime JoinedAt { get; private set; }

    // EF Core
    private PartyMember() { }

    internal PartyMember(Guid partyId, Guid accountId, DateTime joinedAt)
    {
        PartyId = partyId;
        AccountId = accountId;
        IsReady = false;
        JoinedAt = joinedAt;
    }

    internal void SetReady(bool isReady) => IsReady = isReady;
}
