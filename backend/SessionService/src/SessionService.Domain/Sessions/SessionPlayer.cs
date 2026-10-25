namespace SessionService.Domain.Sessions;

public sealed class SessionPlayer
{
    public Guid SessionId { get; private set; }

    public Guid AccountId { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public DateTime? LeftAt { get; private set; }

    public bool IsPresent => LeftAt is null;

    // EF Core
    private SessionPlayer() { }

    internal SessionPlayer(Guid sessionId, Guid accountId, DateTime joinedAt)
    {
        SessionId = sessionId;
        AccountId = accountId;
        JoinedAt = joinedAt;
    }

    internal void Rejoin(DateTime now)
    {
        JoinedAt = now;
        LeftAt = null;
    }

    internal void Leave(DateTime now) => LeftAt ??= now;
}
