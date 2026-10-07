using CatalogService.Domain.Items;

namespace CatalogService.Domain.Versions;

public sealed record CatalogItemSnapshot(
    Guid Id,
    string Code,
    string Kind,
    string Rarity,
    string Status,
    string DisplayName,
    string? Description,
    string? ModelCode,
    string? Slot,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> FitTags,
    long? LastPublishedVersion,
    IReadOnlyList<CatalogItemStatSnapshot> Stats,
    IReadOnlyList<CatalogItemPriceSnapshot> Prices,
    IReadOnlyList<CatalogAvailabilitySnapshot> Availability,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static CatalogItemSnapshot From(Item item) =>
        new(
            item.Id,
            item.Code,
            item.Kind.ToString(),
            item.Rarity.ToString(),
            item.Status.ToString(),
            item.DisplayName,
            item.Description,
            item.ModelCode,
            item.Slot,
            [.. item.Tags],
            [.. item.FitTags],
            item.LastPublishedVersion,
            [.. item.Stats.Select(stat => new CatalogItemStatSnapshot(stat.Key, stat.Value))],
            [.. item.Prices.Select(price => new CatalogItemPriceSnapshot(price.CurrencyCode, price.Amount, price.IsActive, price.ValidFrom))],
            [.. item.Availability.Select(window => new CatalogAvailabilitySnapshot(window.StartsAt, window.EndsAt, window.Channel))],
            item.CreatedAt,
            item.UpdatedAt);

    public Item ToItem(long version)
    {
        Item item = Item.Restore(
            Id,
            Code,
            Enum.Parse<ItemKind>(Kind),
            Enum.Parse<ItemRarity>(Rarity),
            ItemStatus.Published,
            DisplayName,
            Description,
            ModelCode,
            Slot,
            [.. Tags],
            [.. FitTags],
            version,
            CreatedAt,
            UpdatedAt);

        item.ApplySnapshot(this, version);

        return item;
    }
}

public sealed record CatalogItemStatSnapshot(string Key, decimal Value);

public sealed record CatalogItemPriceSnapshot(string CurrencyCode, long Amount, bool IsActive, DateTime? ValidFrom);

public sealed record CatalogAvailabilitySnapshot(DateTime? StartsAt, DateTime? EndsAt, string? Channel);
