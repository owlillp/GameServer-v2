using CatalogService.Contracts.Dtos;
using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;

namespace CatalogService.Core.Features.Items.Models;

internal static class ItemMapper
{
    public static ItemDto ToListDto(Item item)
    {
        var activePrices = item.Prices
            .Where(price => price.IsActive)
            .ToList();

        ItemPrice? cheapest = activePrices
            .OrderBy(price => price.Amount)
            .FirstOrDefault();

        return new ItemDto(
            item.Id,
            item.Code,
            item.Kind.ToString(),
            item.Rarity.ToString(),
            item.DisplayName,
            item.ModelCode,
            item.Slot,
            [.. item.Tags],
            [.. item.FitTags],
            cheapest?.Amount,
            cheapest?.CurrencyCode,
            item.CreatedAt,
            item.UpdatedAt);
    }

    public static ItemDetailDto ToDetailDto(Item item) =>
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
            [.. item.Stats.Select(stat => new ItemStatDto(stat.Key, stat.Value))],
            [.. item.Prices.Select(price => new ItemPriceDto(price.CurrencyCode, price.Amount, price.IsActive, price.ValidFrom))],
            [.. item.Availability.Select(window => new AvailabilityWindowDto(window.StartsAt, window.EndsAt, window.Channel))],
            item.CreatedAt,
            item.UpdatedAt);

    public static CatalogVersionDto ToDto(CatalogVersion version) =>
        new(
            version.Id,
            version.Number,
            version.Note,
            version.PublishedBy,
            version.PublishedAt,
            version.Items.Count);
}
