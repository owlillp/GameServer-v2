namespace CatalogService.Contracts.Requests;

public sealed record ItemPriceRequest(string CurrencyCode, long Amount, bool IsActive, DateTime? ValidFrom);
