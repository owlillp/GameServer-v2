using CSharpFunctionalExtensions;
using SessionService.Domain.Servers;
using Shared.SharedKernel.Errors;

namespace SessionService.Domain.Sessions;

public sealed class Session
{
    private readonly List<SessionPlayer> _players = [];

    public Guid Id { get; private set; }

    public Guid ServerId { get; private set; }

    public SessionKind Kind { get; private set; }

    public string Mode { get; private set; } = null!;

    public string Region { get; private set; } = null!;

    public int MaxPlayers { get; private set; }

    public SessionStatus Status { get; private set; }

    public Guid? MatchId { get; private set; }

    public int PlayerCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public string? CloseReason { get; private set; }

    /// <summary>
    /// When the liveness sweeper last saw this session alive; rotates the sweep over all sessions.
    /// </summary>
    public DateTime? LivenessCheckedAt { get; private set; }

    public IReadOnlyList<SessionPlayer> Players => _players;

    public bool IsOpen => Status is SessionStatus.Allocating or SessionStatus.Active;

    // EF Core
    private Session() { }

    public static Result<Session, Error> Create(
        GameServer server,
        SessionKind kind,
        string mode,
        int maxPlayers,
        Guid? matchId,
        DateTime now)
    {
        string normalizedMode = SessionNames.NormalizeMode(mode);

        if (!server.Supports(normalizedMode))
        {
            return SessionErrors.ModeInvalid;
        }

        if (maxPlayers <= 0 || maxPlayers > server.Capacity)
        {
            return SessionErrors.ServerInvalid;
        }

        if ((kind == SessionKind.Match) != matchId.HasValue)
        {
            return SessionErrors.SessionNotJoinable;
        }

        return new Session
        {
            Id = Guid.CreateVersion7(),
            ServerId = server.Id,
            Kind = kind,
            Mode = normalizedMode,
            Region = server.Region,
            MaxPlayers = maxPlayers,
            Status = SessionStatus.Active,
            MatchId = matchId,
            CreatedAt = now,
        };
    }

    public bool IsOwnedBy(Guid serverId) => ServerId == serverId;

    public bool IsPresent(Guid accountId) => _players.Exists(player => player.AccountId == accountId && player.IsPresent);

    /// <summary>
    /// True if the player has been in the session at any time, even if they already left.
    /// </summary>
    public bool HasPlayed(Guid accountId) => _players.Exists(player => player.AccountId == accountId);

    public bool HasFreeSlot => PlayerCount < MaxPlayers;

    public UnitResult<Error> Join(Guid accountId, DateTime now)
    {
        if (!IsOpen)
        {
            return SessionErrors.SessionClosed;
        }

        SessionPlayer? existing = _players.Find(player => player.AccountId == accountId);
        if (existing is { IsPresent: true })
        {
            return UnitResult.Success<Error>();
        }

        if (!HasFreeSlot)
        {
            return SessionErrors.SessionFull;
        }

        if (existing is null)
        {
            _players.Add(new SessionPlayer(Id, accountId, now));
        }
        else
        {
            existing.Rejoin(now);
        }

        PlayerCount++;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Leave(Guid accountId, DateTime now)
    {
        SessionPlayer? player = _players.Find(entry => entry.AccountId == accountId);
        if (player is null)
        {
            return SessionErrors.PlayerNotInSession;
        }

        if (player.IsPresent)
        {
            player.Leave(now);
            PlayerCount = Math.Max(0, PlayerCount - 1);
        }

        return UnitResult.Success<Error>();
    }

    public void MarkAlive(DateTime now) => LivenessCheckedAt = now;

    /// <summary>
    /// Closes the session and returns the players who were still inside.
    /// </summary>
    public IReadOnlyList<Guid> Close(string reason, DateTime now)
    {
        if (Status == SessionStatus.Closed)
        {
            return [];
        }

        Guid[] present = [.. _players.Where(player => player.IsPresent).Select(player => player.AccountId)];

        foreach (SessionPlayer player in _players)
        {
            player.Leave(now);
        }

        PlayerCount = 0;
        Status = SessionStatus.Closed;
        ClosedAt = now;
        CloseReason = reason.Length > SessionConstants.REASON_MAX_LENGTH
            ? reason[..SessionConstants.REASON_MAX_LENGTH]
            : reason;

        return present;
    }
}
