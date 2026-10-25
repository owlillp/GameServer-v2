namespace SessionService.Domain.Reports;

public sealed record ReportedPlayer(
    Guid AccountId,
    int Team,
    int Score,
    int Kills,
    int Deaths,
    bool IsWinner);

public sealed record MatchReport(
    string ActivityId,
    string Mode,
    DateTime StartedAt,
    DateTime EndedAt,
    IReadOnlyList<ReportedPlayer> Players,
    string? StatsJson)
{
    public TimeSpan Duration => EndedAt - StartedAt;
}
