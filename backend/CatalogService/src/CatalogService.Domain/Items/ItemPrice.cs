using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Items;

public sealed class ItemPrice
{
    public Guid Id { get; private set; }

    public Guid ItemId { get; private set; }

    public string CurrencyCode { get; private set; } = null!;

    public long Amount { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? ValidFrom { get; private set; }

    // EF Core
    private ItemPrice() { }

    private ItemPrice(Guid itemId, string currencyCode, long amount, bool isActive, DateTime? validFrom)
    {
        Id = Guid.CreateVersion7();
        ItemId = itemId;
        CurrencyCode = currencyCode;
        Amount = amount;
        IsActive = isActive;
        ValidFrom = validFrom;
    }

    internal static Result<ItemPrice, Error> Create(
        Guid itemId,
        string currencyCode,
        long amount,
        bool isActive,
        DateTime? validFrom)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return GeneralErrors.ValueIsRequired("currency");
        }

        string normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length > CatalogConstants.CURRENCY_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("currency", max: CatalogConstants.CURRENCY_MAX_LENGTH);
        }

        if (amount < 0)
        {
            return GeneralErrors.ValueIsInvalid("amount");
        }

        return new ItemPrice(itemId, normalized, amount, isActive, validFrom);
    }
}
