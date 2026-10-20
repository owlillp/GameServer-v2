using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using StoreService.Domain.Offers;

namespace StoreService.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid OfferId { get; private set; }

    public string OfferCode { get; private set; } = null!;

    public string OfferName { get; private set; } = null!;

    public string Currency { get; private set; } = null!;

    public long TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public Guid? HoldId { get; private set; }

    public string? FailureReason { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    // EF Core
    private Order() { }

    private Order(
        Guid accountId,
        Guid offerId,
        string offerCode,
        string offerName,
        string currency,
        long totalAmount,
        string? idempotencyKey)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        OfferId = offerId;
        OfferCode = offerCode;
        OfferName = offerName;
        Currency = currency;
        TotalAmount = totalAmount;
        Status = OrderStatus.PENDING;
        IdempotencyKey = idempotencyKey;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Order, Error> CreatePending(
        Guid accountId,
        Offer offer,
        string? idempotencyKey)
    {
        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        var order = new Order(
            accountId,
            offer.Id,
            offer.Code,
            offer.DisplayName,
            offer.PriceCurrency,
            offer.PriceAmount,
            NormalizeIdempotencyKey(idempotencyKey));

        foreach (OfferItem offerItem in offer.Items)
        {
            var itemResult = OrderItem.Create(
                order.Id,
                offerItem.CatalogItemId,
                offerItem.Code,
                offerItem.Kind,
                offerItem.Slot,
                offerItem.Quantity);

            if (itemResult.IsFailure)
            {
                return itemResult.Error;
            }

            order._items.Add(itemResult.Value);
        }

        return order;
    }

    public UnitResult<Error> MarkReserved(Guid holdId)
    {
        if (holdId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("holdId");
        }

        if (Status == OrderStatus.RESERVED && HoldId == holdId)
        {
            return UnitResult.Success<Error>();
        }

        if (Status != OrderStatus.PENDING)
        {
            return StoreErrors.OrderNotReserved;
        }

        HoldId = holdId;
        Status = OrderStatus.RESERVED;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkGrantRequested()
    {
        if (Status != OrderStatus.RESERVED)
        {
            return StoreErrors.OrderNotReserved;
        }

        Status = OrderStatus.GRANT_REQUESTED;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Complete()
    {
        if (Status == OrderStatus.COMPLETED)
        {
            return UnitResult.Success<Error>();
        }

        if (Status != OrderStatus.GRANT_REQUESTED)
        {
            return StoreErrors.OrderNotReserved;
        }

        Status = OrderStatus.COMPLETED;
        CompletedAt = DateTime.UtcNow;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Fail(string reason)
    {
        if (Status == OrderStatus.COMPLETED)
        {
            return StoreErrors.OrderAlreadyCompleted;
        }

        if (Status == OrderStatus.FAILED)
        {
            return UnitResult.Success<Error>();
        }

        string normalized = string.IsNullOrWhiteSpace(reason) ? "Unknown error" : reason.Trim();
        FailureReason = normalized.Length > StoreConstants.REASON_MAX_LENGTH
            ? normalized[..StoreConstants.REASON_MAX_LENGTH]
            : normalized;
        Status = OrderStatus.FAILED;
        Touch();
        return UnitResult.Success<Error>();
    }

    public bool IsTerminal => Status is OrderStatus.COMPLETED or OrderStatus.FAILED;

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        string normalized = key.Trim();
        return normalized.Length > StoreConstants.IDEMPOTENCY_KEY_MAX_LENGTH
            ? normalized[..StoreConstants.IDEMPOTENCY_KEY_MAX_LENGTH]
            : normalized;
    }
}
