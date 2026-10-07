namespace CatalogService.Contracts.Dtos;

public sealed record ItemPriceDto(
    string CurrencyCode,
    long Amount,
    bool IsActive,
    DateTime? ValidFrom);
