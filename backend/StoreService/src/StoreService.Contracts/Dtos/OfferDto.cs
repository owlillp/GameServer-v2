namespace StoreService.Contracts.Dtos;

public sealed record OfferDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Status,
    string PriceCurrency,
    long PriceAmount,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int SortOrder,
    IReadOnlyList<OfferItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);
