namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record ClanMemberPayload(
    Guid ClanId,
    Guid AccountId,
    string? Role,
    string? Reason);

public sealed record ClanDisbandedPayload(Guid ClanId, string Tag);
