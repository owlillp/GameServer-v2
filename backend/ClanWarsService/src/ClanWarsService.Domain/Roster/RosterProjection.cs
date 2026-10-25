namespace ClanWarsService.Domain.Roster;

/// <summary>
/// Copy of a clan from ClanService events: enough to show the leaderboard and to check rosters
/// without calling ClanService.
/// </summary>
public sealed class ClanProjection
{
    public Guid ClanId { get; private set; }

    public string Tag { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public bool IsDisbanded { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private ClanProjection() { }

    public ClanProjection(Guid clanId, string tag, string name, DateTime updatedAt)
    {
        ClanId = clanId;
        Tag = tag;
        Name = name;
        UpdatedAt = updatedAt;
    }

    public void MarkDisbanded(DateTime at)
    {
        IsDisbanded = true;
        UpdatedAt = at;
    }
}

/// <summary>
/// One row per player (a player is in one clan). <see cref="EventAt"/> is the time of the last
/// applied event: an older event arriving late is ignored, so redelivery and reordering are safe.
/// </summary>
public sealed class RosterEntry
{
    public Guid AccountId { get; private set; }

    public Guid ClanId { get; private set; }

    public string Role { get; private set; } = null!;

    public DateTime EventAt { get; private set; }

    // EF Core
    private RosterEntry() { }

    public RosterEntry(Guid accountId, Guid clanId, string role, DateTime eventAt)
    {
        AccountId = accountId;
        ClanId = clanId;
        Role = role;
        EventAt = eventAt;
    }

    public bool IsNewerThan(DateTime eventAt) => EventAt > eventAt;

    public void Apply(Guid clanId, string role, DateTime eventAt)
    {
        ClanId = clanId;
        Role = role;
        EventAt = eventAt;
    }
}
