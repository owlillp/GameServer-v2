using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace StoreService.Domain.Offers;

public sealed class OfferItem
{
    public Guid Id { get; private set; }

    public Guid OfferId { get; private set; }

    public Guid CatalogItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string? Slot { get; private set; }

    public int Quantity { get; private set; }

    // EF Core
    private OfferItem() { }

    private OfferItem(
        Guid offerId,
        Guid catalogItemId,
        string code,
        string kind,
        string? slot,
        int quantity)
    {
        Id = Guid.CreateVersion7();
        OfferId = offerId;
        CatalogItemId = catalogItemId;
        Code = code;
        Kind = kind;
        Slot = slot;
        Quantity = quantity;
    }

    internal static Result<OfferItem, Error> Create(
        Guid offerId,
        Guid catalogItemId,
        string code,
        string kind,
        string? slot,
        int quantity)
    {
        if (catalogItemId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("itemId");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(kind))
        {
            return GeneralErrors.ValueIsRequired("items");
        }

        if (quantity <= 0)
        {
            return StoreErrors.QuantityInvalid;
        }

        return new OfferItem(
            offerId,
            catalogItemId,
            code.Trim(),
            kind.Trim(),
            string.IsNullOrWhiteSpace(slot) ? null : slot.Trim(),
            quantity);
    }
}
