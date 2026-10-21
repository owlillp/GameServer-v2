namespace Shared.IntegrationEvents.Matchmaking.Commands;

public sealed record GrantMatchRewardCommand(
    Guid GrantId,
    Guid MatchId,
    Guid AccountId,
    IReadOnlyList<GrantMatchRewardItem> Items,
    DateTime RequestedAt);

public sealed record GrantMatchRewardItem(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
