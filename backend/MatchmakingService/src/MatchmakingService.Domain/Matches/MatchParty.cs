namespace MatchmakingService.Domain.Matches;

public sealed class MatchParty
{
    public Guid MatchId { get; private set; }

    public Guid PartyId { get; private set; }

    public int Side { get; private set; }

    // EF Core
    private MatchParty() { }

    internal MatchParty(Guid matchId, Guid partyId, int side)
    {
        MatchId = matchId;
        PartyId = partyId;
        Side = side;
    }
}
