using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanService.Domain.Requests;

public sealed class ClanJoinRequest
{
    public Guid Id { get; private set; }

    public Guid ClanId { get; private set; }

    public Guid AccountId { get; private set; }

    public ClanRequestKind Kind { get; private set; }

    public ClanRequestStatus Status { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Guid? ResolvedBy { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public bool IsPending => Status == ClanRequestStatus.Pending;

    // EF Core
    private ClanJoinRequest() { }

    public ClanJoinRequest(Guid clanId, Guid accountId, ClanRequestKind kind, Guid createdBy, DateTime now)
    {
        Id = Guid.CreateVersion7();
        ClanId = clanId;
        AccountId = accountId;
        Kind = kind;
        Status = ClanRequestStatus.Pending;
        CreatedBy = createdBy;
        CreatedAt = now;
    }

    /// <summary>
    /// The side that decides: the invited player for an invite, the clan (officer+) for a request.
    /// </summary>
    public bool IsDecidedByPlayer => Kind == ClanRequestKind.Invite;

    public UnitResult<Error> Accept(Guid actor, DateTime now) => Resolve(ClanRequestStatus.Accepted, actor, now);

    public UnitResult<Error> Decline(Guid actor, DateTime now) => Resolve(ClanRequestStatus.Declined, actor, now);

    public UnitResult<Error> Cancel(Guid actor, DateTime now) => Resolve(ClanRequestStatus.Cancelled, actor, now);

    private UnitResult<Error> Resolve(ClanRequestStatus status, Guid actor, DateTime now)
    {
        if (Status != ClanRequestStatus.Pending)
        {
            return ClanErrors.RequestNotPending;
        }

        Status = status;
        ResolvedBy = actor;
        ResolvedAt = now;

        return UnitResult.Success<Error>();
    }
}
