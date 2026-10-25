namespace SessionService.Domain.Reports;

/// <summary>
/// The report exactly as the game server sent it, kept for disputes and anti-cheat review.
/// <see cref="ActivityId"/> is unique: a retried report is recognised and not published twice.
/// </summary>
public sealed class RawReport
{
    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public Guid ServerId { get; private set; }

    public string ActivityId { get; private set; } = null!;

    public Guid MatchId { get; private set; }

    public string Mode { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public IReadOnlyList<string> Flags { get; private set; } = [];

    public bool IsSuspicious { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    // EF Core
    private RawReport() { }

    public RawReport(
        Guid sessionId,
        Guid serverId,
        string activityId,
        Guid matchId,
        string mode,
        string payload,
        IReadOnlyList<string> flags,
        DateTime receivedAt)
    {
        Id = Guid.CreateVersion7();
        SessionId = sessionId;
        ServerId = serverId;
        ActivityId = activityId;
        MatchId = matchId;
        Mode = SessionNames.NormalizeMode(mode);
        Payload = payload;
        Flags = [.. flags.Select(flag => flag.Length > SessionConstants.FLAG_MAX_LENGTH
            ? flag[..SessionConstants.FLAG_MAX_LENGTH]
            : flag)];
        IsSuspicious = flags.Count > 0;
        ReceivedAt = receivedAt;
    }
}
