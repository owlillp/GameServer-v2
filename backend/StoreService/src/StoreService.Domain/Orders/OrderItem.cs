using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace StoreService.Domain.Orders;

public sealed class OrderItem
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid CatalogItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string? Slot { get; private set; }

    public int Quantity { get; private set; }

    // EF Core
    private OrderItem() { }

    private OrderItem(
        Guid orderId,
        Guid catalogItemId,
        string code,
        string kind,
        string? slot,
        int quantity)
    {
        Id = Guid.CreateVersion7();
        OrderId = orderId;
        CatalogItemId = catalogItemId;
        Code = code;
        Kind = kind;
        Slot = slot;
        Quantity = quantity;
    }

    internal static Result<OrderItem, Error> Create(
        Guid orderId,
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

        if (quantity <= 0)
        {
            return StoreErrors.QuantityInvalid;
        }

        return new OrderItem(
            orderId,
            catalogItemId,
            code.Trim(),
            kind.Trim(),
            string.IsNullOrWhiteSpace(slot) ? null : slot.Trim(),
            quantity);
    }
}
