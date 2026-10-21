namespace MatchmakingService.Domain.Matches;

public sealed class MatchResult
{
    public Guid Id { get; private set; }

    public Guid MatchId { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid PartyId { get; private set; }

    public long Score { get; private set; }

    public int Kills { get; private set; }

    public bool IsWin { get; private set; }

    public DateTime SubmittedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private MatchResult() { }

    internal MatchResult(Guid matchId, Guid accountId, Guid partyId, long score, int kills, DateTime now)
    {
        Id = Guid.CreateVersion7();
        MatchId = matchId;
        AccountId = accountId;
        PartyId = partyId;
        Score = score;
        Kills = kills;
        SubmittedAt = now;
        UpdatedAt = now;
    }

    internal void Update(long score, int kills, DateTime now)
    {
        Score = score;
        Kills = kills;
        UpdatedAt = now;
    }

    internal void SetWin(bool isWin) => IsWin = isWin;
}
