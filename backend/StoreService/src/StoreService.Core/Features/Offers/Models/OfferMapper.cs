using StoreService.Contracts.Dtos;
using StoreService.Domain.Offers;

namespace StoreService.Core.Features.Offers.Models;

internal static class OfferMapper
{
    public static OfferDto ToDto(Offer offer) =>
        new(
            offer.Id,
            offer.Code,
            offer.DisplayName,
            offer.Description,
            offer.Status.ToString(),
            offer.PriceCurrency,
            offer.PriceAmount,
            offer.StartsAt,
            offer.EndsAt,
            offer.SortOrder,
            [.. offer.Items.Select(ToItemDto)],
            offer.CreatedAt,
            offer.UpdatedAt);

    private static OfferItemDto ToItemDto(OfferItem item) =>
        new(item.CatalogItemId, item.Code, item.Kind, item.Slot, item.Quantity);
}
