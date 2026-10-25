namespace ClanWarsService.Domain.Seasons;

public sealed record BattleWindowTemplate(TimeOnly DailyStartUtc, TimeOnly DailyEndUtc);

public sealed class BattleWindow
{
    public Guid Id { get; private set; }

    public Guid SeasonId { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    // EF Core
    private BattleWindow() { }

    public BattleWindow(Guid seasonId, DateTime startsAt, DateTime endsAt)
    {
        Id = Guid.CreateVersion7();
        SeasonId = seasonId;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public bool IsOpen(DateTime now) => StartsAt <= now && now < EndsAt;
}
