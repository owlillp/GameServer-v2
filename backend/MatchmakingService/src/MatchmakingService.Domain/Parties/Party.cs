using CSharpFunctionalExtensions;
using MatchmakingService.Domain.Queues;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Domain.Parties;

public sealed class Party
{
    private readonly List<PartyMember> _members = [];

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public Guid LeaderAccountId { get; private set; }

    public PartyStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public uint Version { get; private set; }

    public IReadOnlyList<PartyMember> Members => _members;

    // EF Core
    private Party() { }

    private Party(Guid leaderAccountId, DateTime now)
    {
        Id = Guid.CreateVersion7();
        Code = Id.ToString("N")[..MatchmakingConstants.PARTY_CODE_LENGTH].ToUpperInvariant();
        LeaderAccountId = leaderAccountId;
        Status = PartyStatus.Forming;
        CreatedAt = now;
        UpdatedAt = now;
        Version = 1;
        _members.Add(new PartyMember(Id, leaderAccountId, now));
    }

    public static Result<Party, Error> Create(Guid leaderAccountId, DateTime now)
    {
        if (leaderAccountId == Guid.Empty)
        {
            return MatchmakingErrors.PartyMemberNotFound;
        }

        return new Party(leaderAccountId, now);
    }

    public bool IsMember(Guid accountId) => _members.Exists(member => member.AccountId == accountId);

    public bool AllReady => _members.Count > 0 && _members.TrueForAll(member => member.IsReady);

    public bool CanQueue =>
        Status == PartyStatus.Forming
        && _members.Count >= MatchmakingConstants.MIN_PARTY_SIZE
        && AllReady;

    public UnitResult<Error> Join(Guid accountId, DateTime now)
    {
        if (Status != PartyStatus.Forming)
        {
            return MatchmakingErrors.PartyClosed;
        }

        if (_members.Exists(member => member.AccountId == accountId))
        {
            return MatchmakingErrors.PartyAlreadyMember;
        }

        if (_members.Count >= MatchmakingConstants.MAX_PARTY_SIZE)
        {
            return MatchmakingErrors.PartyFull;
        }

        _members.Add(new PartyMember(Id, accountId, now));
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Leave(Guid accountId, DateTime now)
    {
        PartyMember? member = _members.Find(entry => entry.AccountId == accountId);
        if (member is null)
        {
            return MatchmakingErrors.PartyMemberNotFound;
        }

        if (Status is PartyStatus.Queued or PartyStatus.InMatch)
        {
            return MatchmakingErrors.PartyInMatch;
        }

        _members.Remove(member);

        if (_members.Count == 0)
        {
            Status = PartyStatus.Closed;
        }
        else if (LeaderAccountId == accountId)
        {
            LeaderAccountId = _members
                .OrderBy(entry => entry.JoinedAt)
                .First()
                .AccountId;
        }

        UpdatedAt = now;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Kick(Guid leaderAccountId, Guid accountId, DateTime now)
    {
        if (LeaderAccountId != leaderAccountId)
        {
            return MatchmakingErrors.PartyNotLeader;
        }

        if (accountId == leaderAccountId)
        {
            return MatchmakingErrors.PartyMemberNotFound;
        }

        PartyMember? member = _members.Find(entry => entry.AccountId == accountId);
        if (member is null)
        {
            return MatchmakingErrors.PartyMemberNotFound;
        }

        if (Status is PartyStatus.Queued or PartyStatus.InMatch)
        {
            return MatchmakingErrors.PartyInMatch;
        }

        _members.Remove(member);
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SetReady(Guid accountId, bool isReady, DateTime now)
    {
        if (Status != PartyStatus.Forming)
        {
            return MatchmakingErrors.PartyClosed;
        }

        PartyMember? member = _members.Find(entry => entry.AccountId == accountId);
        if (member is null)
        {
            return MatchmakingErrors.PartyMemberNotFound;
        }

        member.SetReady(isReady);
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkQueued(Guid leaderAccountId, DateTime now)
    {
        if (LeaderAccountId != leaderAccountId)
        {
            return MatchmakingErrors.PartyNotLeader;
        }

        if (Status == PartyStatus.Queued)
        {
            return MatchmakingErrors.PartyAlreadyQueued;
        }

        if (Status == PartyStatus.InMatch)
        {
            return MatchmakingErrors.PartyInMatch;
        }

        if (Status != PartyStatus.Forming)
        {
            return MatchmakingErrors.PartyClosed;
        }

        if (_members.Count is < MatchmakingConstants.MIN_PARTY_SIZE or > MatchmakingConstants.MAX_PARTY_SIZE)
        {
            return MatchmakingErrors.PartySizeInvalid;
        }

        if (!AllReady)
        {
            return MatchmakingErrors.PartyNotReady;
        }

        Status = PartyStatus.Queued;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkInMatch(DateTime now)
    {
        if (Status == PartyStatus.InMatch)
        {
            return MatchmakingErrors.PartyInMatch;
        }

        if (Status == PartyStatus.Closed)
        {
            return MatchmakingErrors.PartyClosed;
        }

        if (_members.Count < MatchmakingConstants.MIN_PARTY_SIZE)
        {
            return MatchmakingErrors.PartySizeInvalid;
        }

        Status = PartyStatus.InMatch;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ReleaseFromQueue(DateTime now)
    {
        if (Status != PartyStatus.Queued)
        {
            return MatchmakingErrors.PartyNotQueued;
        }

        Status = PartyStatus.Forming;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ReleaseFromMatch(DateTime now)
    {
        if (Status != PartyStatus.InMatch)
        {
            return MatchmakingErrors.PartyNotInMatch;
        }

        foreach (PartyMember member in _members)
        {
            member.SetReady(false);
        }

        Status = PartyStatus.Forming;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Close(DateTime now)
    {
        Status = PartyStatus.Closed;
        UpdatedAt = now;

        return UnitResult.Success<Error>();
    }

    public IReadOnlyList<Guid> MemberAccountIds =>
        [.. _members.Select(member => member.AccountId)];
}
