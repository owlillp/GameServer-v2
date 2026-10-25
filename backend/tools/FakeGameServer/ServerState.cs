using System.Collections.Concurrent;

namespace FakeGameServer;

public sealed class FakeSession(Guid id, string kind, string mode, Guid? matchId, DateTime createdAt)
{
    public Guid Id { get; } = id;

    public string Kind { get; } = kind;

    public string Mode { get; } = mode;

    public Guid? MatchId { get; } = matchId;

    public DateTime LastActivityAt { get; set; } = createdAt;

    public ConcurrentDictionary<Guid, DateTime> Players { get; } = new();
}

/// <summary>
/// In-memory state of the fake server: who is connected to which session.
/// </summary>
public sealed class ServerState
{
    private volatile string _serverId = string.Empty;

    public string ServerId
    {
        get => _serverId;
        set => _serverId = value;
    }

    public ConcurrentDictionary<Guid, FakeSession> Sessions { get; } = new();

    public IReadOnlyList<Guid> LiveSessionIds(DateTime now, TimeSpan emptyTimeout) =>
        [.. Sessions.Values
            .Where(session => !session.Players.IsEmpty || now - session.LastActivityAt < emptyTimeout)
            .Select(session => session.Id)];

    public void DropIdle(DateTime now, TimeSpan emptyTimeout)
    {
        foreach (FakeSession session in Sessions.Values)
        {
            if (session.Players.IsEmpty && now - session.LastActivityAt >= emptyTimeout)
            {
                Sessions.TryRemove(session.Id, out _);
            }
        }
    }
}
