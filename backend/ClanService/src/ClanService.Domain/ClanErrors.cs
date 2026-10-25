using Shared.SharedKernel.Errors;

namespace ClanService.Domain;

public static class ClanErrors
{
    public static Error TagInvalid { get; } =
        Error.Validation("clan.tag.invalid", "Tag must be 3-6 latin letters or digits", "tag");

    public static Error NameInvalid { get; } =
        Error.Validation("clan.name.invalid", "Name must be 3-32 characters", "name");

    public static Error DescriptionInvalid { get; } =
        Error.Validation("clan.description.invalid", "Description is too long", "description");

    public static Error TagTaken { get; } =
        Error.Conflict("clan.tag.taken", "A clan with this tag already exists", "tag");

    public static Error ClanNotFound { get; } =
        Error.NotFound("clan.not_found", "Clan was not found", "clanId");

    public static Error ClanDisbanded { get; } =
        Error.Conflict("clan.disbanded", "Clan is disbanded", "clanId");

    public static Error ClanFull { get; } =
        Error.Conflict("clan.full", "Clan has no free slots", "clanId");

    public static Error AlreadyInClan { get; } =
        Error.Conflict("clan.member.already_in_clan", "Player is already in a clan", "accountId");

    public static Error NotInClan { get; } =
        Error.NotFound("clan.member.not_in_clan", "Player is not in this clan", "accountId");

    public static Error JoinCooldown { get; } =
        Error.Conflict("clan.member.cooldown", "Player left a clan recently and cannot join yet", "accountId");

    public static Error Forbidden { get; } =
        Error.Authorization("clan.forbidden", "Your clan role does not allow this action", "role");

    public static Error LeaderMustTransfer { get; } =
        Error.Conflict("clan.leader.must_transfer", "Transfer leadership before leaving the clan", "accountId");

    public static Error RoleInvalid { get; } =
        Error.Validation("clan.role.invalid", "Role is invalid", "role");

    public static Error RequestNotFound { get; } =
        Error.NotFound("clan.request.not_found", "Join request or invite was not found", "requestId");

    public static Error RequestNotPending { get; } =
        Error.Conflict("clan.request.not_pending", "Join request or invite is already resolved", "requestId");

    public static Error RequestDuplicate { get; } =
        Error.Conflict("clan.request.duplicate", "A pending request or invite already exists", "accountId");

    public static Error PointsAmountInvalid { get; } =
        Error.Validation("clan.points.amount.invalid", "Amount must not be zero", "amount");

    public static Error PointsReasonInvalid { get; } =
        Error.Validation("clan.points.reason.invalid", "Reason is required", "reason");

    public static Error PointsAlreadyApplied { get; } =
        Error.Conflict("clan.points.duplicate", "Operation has already been applied", "operationId");

    public static Error CreationFeeFailed { get; } =
        Error.Conflict("clan.fee.failed", "Not enough currency to create a clan", "fee");
}
