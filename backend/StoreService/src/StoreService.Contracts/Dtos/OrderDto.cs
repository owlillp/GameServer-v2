namespace StoreService.Contracts.Dtos;

public sealed record OrderDto(
    Guid Id,
    Guid AccountId,
    Guid OfferId,
    string OfferCode,
    string OfferName,
    string Currency,
    long TotalAmount,
    string Status,
    string? FailureReason,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? CompletedAt);
