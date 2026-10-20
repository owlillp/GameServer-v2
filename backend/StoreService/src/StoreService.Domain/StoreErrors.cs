using Shared.SharedKernel.Errors;

namespace StoreService.Domain;

public static class StoreErrors
{
    public static Error OfferNotFound { get; } =
        Error.Validation("store.offer.not_found", "Offer was not found", "offerId");

    public static Error OfferCodeDuplicate { get; } =
        Error.Conflict("store.offer.code.duplicate", "Offer code already exists", "code");

    public static Error OfferNotAvailable { get; } =
        Error.Validation("store.offer.not_available", "Offer is not available for purchase", "offerId");

    public static Error OfferItemsRequired { get; } =
        Error.Validation("store.offer.items.required", "Offer must contain at least one item", "items");

    public static Error OfferItemsDuplicate { get; } =
        Error.Validation("store.offer.items.duplicate", "Offer contains duplicate items", "items");

    public static Error OfferArchived { get; } =
        Error.Validation("store.offer.archived", "Archived offer cannot be changed", "offerId");

    public static Error OrderNotFound { get; } =
        Error.Validation("store.order.not_found", "Order was not found", "orderId");

    public static Error OrderNotReserved { get; } =
        Error.Validation("store.order.not_reserved", "Order is not waiting for items", "orderId");

    public static Error OrderAlreadyCompleted { get; } =
        Error.Validation("store.order.completed", "Order has already been completed", "orderId");

    public static Error QuantityInvalid { get; } =
        Error.Validation("store.quantity.invalid", "Quantity must be positive", "quantity");

    public static Error PriceInvalid { get; } =
        Error.Validation("store.price.invalid", "Price must be greater than zero", "priceAmount");

    public static Error CatalogItemUnavailable(Guid itemId) =>
        Error.Validation("store.catalog.item_unavailable", $"Catalog item '{itemId}' is not available", "items");

    public static Error WalletReservationFailed(string reason) =>
        Error.Validation("store.wallet.reservation_failed", reason, "wallet");

    public static Error ItemsGrantFailed(string reason) =>
        Error.Validation("store.items.grant_failed", reason, "items");
}
