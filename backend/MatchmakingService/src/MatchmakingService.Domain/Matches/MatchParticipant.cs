namespace MatchmakingService.Domain.Matches;

public sealed class MatchParticipant
{
    public Guid Id { get; private set; }

    public Guid MatchId { get; private set; }

    public Guid PartyId { get; private set; }

    public Guid AccountId { get; private set; }

    // EF Core
    private MatchParticipant() { }

    internal MatchParticipant(Guid matchId, Guid partyId, Guid accountId)
    {
        Id = Guid.CreateVersion7();
        MatchId = matchId;
        PartyId = partyId;
        AccountId = accountId;
    }
}
