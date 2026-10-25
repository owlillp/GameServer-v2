namespace Shared.IntegrationEvents.ClanWars.Commands;

/// <summary>
/// Season reward items for one player. Inventory grants them once per <see cref="GrantId"/>.
/// </summary>
public sealed record GrantSeasonRewardItemsCommand(
    Guid GrantId,
    Guid SeasonId,
    Guid AccountId,
    IReadOnlyList<GrantSeasonRewardItem> Items,
    DateTime RequestedAt);

public sealed record GrantSeasonRewardItem(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
