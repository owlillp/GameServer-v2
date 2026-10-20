using StoreService.Contracts.Dtos;
using StoreService.Domain.Orders;

namespace StoreService.Core.Features.Orders.Models;

internal static class OrderMapper
{
    public static OrderDto ToDto(Order order) =>
        new(
            order.Id,
            order.AccountId,
            order.OfferId,
            order.OfferCode,
            order.OfferName,
            order.Currency,
            order.TotalAmount,
            order.Status.ToString(),
            order.FailureReason,
            [.. order.Items.Select(ToItemDto)],
            order.CreatedAt,
            order.CompletedAt);

    private static OrderItemDto ToItemDto(OrderItem item) =>
        new(item.CatalogItemId, item.Code, item.Kind, item.Slot, item.Quantity);
}
