using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Requests;
using CatalogService.Contracts.Communication;
using StoreService.Domain.Offers;

namespace StoreService.Core.Features.Offers.Models;

internal static class OfferItemsFactory
{
    public static async Task<Result<IReadOnlyList<OfferItemData>, Error>> BuildAsync(
        IReadOnlyList<OfferItemRequest> items,
        ICatalogClient catalogClient,
        CancellationToken cancellationToken)
    {
        var result = new List<OfferItemData>(items.Count);

        foreach (OfferItemRequest item in items)
        {
            Result<CatalogItemSnapshot, Error> catalogResult = await catalogClient.GetItemAsync(
                item.CatalogItemId,
                cancellationToken);

            if (catalogResult.IsFailure)
            {
                return catalogResult.Error;
            }

            CatalogItemSnapshot snapshot = catalogResult.Value;
            result.Add(new OfferItemData(
                snapshot.Id,
                snapshot.Code,
                snapshot.Kind,
                snapshot.Slot,
                item.Quantity));
        }

        return result;
    }
}
