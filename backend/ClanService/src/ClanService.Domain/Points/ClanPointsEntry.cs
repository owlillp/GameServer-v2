using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanService.Domain.Points;

/// <summary>
/// Every change of clan points. <see cref="OperationId"/> is unique: a repeated operation
/// (redelivered event, retried admin call) is applied once.
/// </summary>
public sealed class ClanPointsEntry
{
    public Guid Id { get; private set; }

    public Guid ClanId { get; private set; }

    public Guid OperationId { get; private set; }

    public long Amount { get; private set; }

    public string Reason { get; private set; } = null!;

    public Guid? ActorId { get; private set; }

    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private ClanPointsEntry() { }

    public static Result<ClanPointsEntry, Error> Create(
        Guid clanId,
        Guid operationId,
        long amount,
        string reason,
        Guid? actorId,
        string? comment,
        DateTime now)
    {
        if (amount == 0)
        {
            return ClanErrors.PointsAmountInvalid;
        }

        string trimmedReason = (reason ?? string.Empty).Trim();
        if (trimmedReason.Length is 0 or > ClanConstants.REASON_MAX_LENGTH
            || (comment?.Length ?? 0) > ClanConstants.COMMENT_MAX_LENGTH)
        {
            return ClanErrors.PointsReasonInvalid;
        }

        return new ClanPointsEntry
        {
            Id = Guid.CreateVersion7(),
            ClanId = clanId,
            OperationId = operationId == Guid.Empty ? Guid.CreateVersion7() : operationId,
            Amount = amount,
            Reason = trimmedReason,
            ActorId = actorId,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = now,
        };
    }
}
