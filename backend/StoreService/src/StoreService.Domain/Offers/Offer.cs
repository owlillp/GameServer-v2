using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace StoreService.Domain.Offers;

public sealed class Offer
{
    private readonly List<OfferItem> _items = [];

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string? Description { get; private set; }

    public OfferStatus Status { get; private set; }

    public string PriceCurrency { get; private set; } = null!;

    public long PriceAmount { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? EndsAt { get; private set; }

    public int SortOrder { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<OfferItem> Items => _items;

    // EF Core
    private Offer() { }

    private Offer(
        string code,
        string displayName,
        string? description,
        string priceCurrency,
        long priceAmount,
        DateTime? startsAt,
        DateTime? endsAt,
        int sortOrder)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        DisplayName = displayName;
        Description = description;
        Status = OfferStatus.DRAFT;
        PriceCurrency = priceCurrency;
        PriceAmount = priceAmount;
        StartsAt = startsAt;
        EndsAt = endsAt;
        SortOrder = sortOrder;
        Version = 1;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Offer, Error> Create(
        string code,
        string displayName,
        string? description,
        string priceCurrency,
        long priceAmount,
        DateTime? startsAt,
        DateTime? endsAt,
        int sortOrder,
        IReadOnlyList<OfferItemData> items)
    {
        var codeResult = NormalizeCode(code);
        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        var nameResult = NormalizeDisplayName(displayName);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var descriptionResult = NormalizeDescription(description);
        if (descriptionResult.IsFailure)
        {
            return descriptionResult.Error;
        }

        var currencyResult = NormalizeCurrency(priceCurrency);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        if (priceAmount <= 0)
        {
            return StoreErrors.PriceInvalid;
        }

        var itemsResult = ValidateItems(items);
        if (itemsResult.IsFailure)
        {
            return itemsResult.Error;
        }

        var offer = new Offer(
            codeResult.Value,
            nameResult.Value,
            descriptionResult.Value,
            currencyResult.Value,
            priceAmount,
            startsAt,
            endsAt,
            sortOrder);

        var addResult = offer.ReplaceItems(items);
        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        return offer;
    }

    public UnitResult<Error> Update(
        string displayName,
        string? description,
        string priceCurrency,
        long priceAmount,
        DateTime? startsAt,
        DateTime? endsAt,
        int sortOrder,
        IReadOnlyList<OfferItemData> items)
    {
        if (Status == OfferStatus.ARCHIVED)
        {
            return StoreErrors.OfferArchived;
        }

        var nameResult = NormalizeDisplayName(displayName);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var descriptionResult = NormalizeDescription(description);
        if (descriptionResult.IsFailure)
        {
            return descriptionResult.Error;
        }

        var currencyResult = NormalizeCurrency(priceCurrency);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        if (priceAmount <= 0)
        {
            return StoreErrors.PriceInvalid;
        }

        var itemsResult = ReplaceItems(items);
        if (itemsResult.IsFailure)
        {
            return itemsResult.Error;
        }

        DisplayName = nameResult.Value;
        Description = descriptionResult.Value;
        PriceCurrency = currencyResult.Value;
        PriceAmount = priceAmount;
        StartsAt = startsAt;
        EndsAt = endsAt;
        SortOrder = sortOrder;
        Status = Status == OfferStatus.ACTIVE ? OfferStatus.DRAFT : Status;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Activate()
    {
        if (Status == OfferStatus.ARCHIVED)
        {
            return StoreErrors.OfferArchived;
        }

        Status = OfferStatus.ACTIVE;
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Archive()
    {
        if (Status == OfferStatus.ARCHIVED)
        {
            return UnitResult.Success<Error>();
        }

        Status = OfferStatus.ARCHIVED;
        Touch();
        return UnitResult.Success<Error>();
    }

    public bool IsAvailable(DateTime now) =>
        Status == OfferStatus.ACTIVE
        && (StartsAt is null || StartsAt <= now)
        && (EndsAt is null || EndsAt >= now);

    private UnitResult<Error> ReplaceItems(IReadOnlyList<OfferItemData> items)
    {
        var itemsResult = ValidateItems(items);
        if (itemsResult.IsFailure)
        {
            return itemsResult.Error;
        }

        _items.Clear();
        foreach (OfferItemData data in items)
        {
            var itemResult = OfferItem.Create(
                Id,
                data.CatalogItemId,
                data.Code,
                data.Kind,
                data.Slot,
                data.Quantity);

            if (itemResult.IsFailure)
            {
                return itemResult.Error;
            }

            _items.Add(itemResult.Value);
        }

        return UnitResult.Success<Error>();
    }

    private static UnitResult<Error> ValidateItems(IReadOnlyList<OfferItemData> items)
    {
        if (items.Count == 0)
        {
            return StoreErrors.OfferItemsRequired;
        }

        if (items.Count > StoreConstants.MAX_OFFER_ITEMS)
        {
            return GeneralErrors.ValueIsInvalid("items");
        }

        if (items.Select(item => item.CatalogItemId).Distinct().Count() != items.Count)
        {
            return StoreErrors.OfferItemsDuplicate;
        }

        return UnitResult.Success<Error>();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    private static Result<string, Error> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired("code");
        }

        string normalized = code.Trim().ToLowerInvariant();
        if (normalized.Length > StoreConstants.CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("code", max: StoreConstants.CODE_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string, Error> NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return GeneralErrors.ValueIsRequired("displayName");
        }

        string normalized = displayName.Trim();
        if (normalized.Length > StoreConstants.DISPLAY_NAME_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("displayName", max: StoreConstants.DISPLAY_NAME_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string?, Error> NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return (string?)null;
        }

        string normalized = description.Trim();
        if (normalized.Length > StoreConstants.DESCRIPTION_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("description", max: StoreConstants.DESCRIPTION_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string, Error> NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return GeneralErrors.ValueIsRequired("priceCurrency");
        }

        string normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length > StoreConstants.CURRENCY_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("priceCurrency", max: StoreConstants.CURRENCY_MAX_LENGTH);
        }

        return normalized;
    }
}
