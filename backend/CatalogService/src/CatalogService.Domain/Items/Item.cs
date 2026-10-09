using CSharpFunctionalExtensions;
using CatalogService.Domain.Versions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Items;

public sealed class Item
{
    private readonly List<ItemStat> _stats = [];
    private readonly List<ItemPrice> _prices = [];
    private readonly List<AvailabilityWindow> _availability = [];

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public ItemKind Kind { get; private set; }

    public ItemRarity Rarity { get; private set; }

    public ItemStatus Status { get; private set; }

    public string DisplayName { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? ModelCode { get; private set; }

    public string? Slot { get; private set; }

    public string[] Tags { get; private set; } = [];

    public string[] FitTags { get; private set; } = [];

    public long? LastPublishedVersion { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ItemStat> Stats => _stats;

    public IReadOnlyList<ItemPrice> Prices => _prices;

    public IReadOnlyList<AvailabilityWindow> Availability => _availability;

    // EF Core
    private Item() { }

    private Item(
        Guid id,
        string code,
        ItemKind kind,
        ItemRarity rarity,
        string displayName,
        string? description,
        string? modelCode,
        string? slot,
        string[] tags,
        string[] fitTags)
    {
        Id = id;
        Code = code;
        Kind = kind;
        Rarity = rarity;
        Status = ItemStatus.DRAFT;
        DisplayName = displayName;
        Description = description;
        ModelCode = modelCode;
        Slot = slot;
        Tags = tags;
        FitTags = fitTags;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Result<Item, Error> Create(
        string code,
        ItemKind kind,
        ItemRarity rarity,
        string displayName,
        string? description,
        string? modelCode,
        string? slot,
        IReadOnlyList<string>? tags,
        IReadOnlyList<string>? fitTags)
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

        var modelResult = NormalizeModelCode(kind, modelCode);
        if (modelResult.IsFailure)
        {
            return modelResult.Error;
        }

        var slotResult = NormalizeSlot(slot);
        if (slotResult.IsFailure)
        {
            return slotResult.Error;
        }

        var tagsResult = NormalizeTags(tags);
        if (tagsResult.IsFailure)
        {
            return tagsResult.Error;
        }

        var fitResult = NormalizeFitTags(kind, fitTags);
        if (fitResult.IsFailure)
        {
            return fitResult.Error;
        }

        return new Item(
            Guid.CreateVersion7(),
            codeResult.Value,
            kind,
            rarity,
            nameResult.Value,
            descriptionResult.Value,
            modelResult.Value,
            slotResult.Value,
            tagsResult.Value,
            fitResult.Value);
    }

    public UnitResult<Error> Update(
        string displayName,
        string? description,
        ItemRarity rarity,
        string? modelCode,
        string? slot,
        IReadOnlyList<string>? tags,
        IReadOnlyList<string>? fitTags)
    {
        if (Status == ItemStatus.ARCHIVED)
        {
            return CatalogErrors.ItemAlreadyArchived;
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

        var modelResult = NormalizeModelCode(Kind, modelCode);
        if (modelResult.IsFailure)
        {
            return modelResult.Error;
        }

        var slotResult = NormalizeSlot(slot);
        if (slotResult.IsFailure)
        {
            return slotResult.Error;
        }

        var tagsResult = NormalizeTags(tags);
        if (tagsResult.IsFailure)
        {
            return tagsResult.Error;
        }

        var fitResult = NormalizeFitTags(Kind, fitTags);
        if (fitResult.IsFailure)
        {
            return fitResult.Error;
        }

        DisplayName = nameResult.Value;
        Description = descriptionResult.Value;
        Rarity = rarity;
        ModelCode = modelResult.Value;
        Slot = slotResult.Value;
        Tags = tagsResult.Value;
        FitTags = fitResult.Value;
        Status = ItemStatus.DRAFT;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Archive()
    {
        if (Status == ItemStatus.ARCHIVED)
        {
            return CatalogErrors.ItemAlreadyArchived;
        }

        Status = ItemStatus.ARCHIVED;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reactivate()
    {
        if (Status != ItemStatus.ARCHIVED)
        {
            return CatalogErrors.ItemNotArchived;
        }

        Status = ItemStatus.DRAFT;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public void MarkPublished(long version)
    {
        Status = ItemStatus.PUBLISHED;
        LastPublishedVersion = version;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStat(string key, decimal value)
    {
        var existing = _stats.FirstOrDefault(stat => string.Equals(stat.Key, key, StringComparison.Ordinal));
        if (existing is not null)
        {
            _stats.Remove(existing);
        }

        var created = ItemStat.Create(Id, key, value);
        if (created.IsSuccess)
        {
            _stats.Add(created.Value);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetPrice(string currencyCode, long amount, bool isActive, DateTime? validFrom)
    {
        _prices.RemoveAll(price => string.Equals(price.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

        var created = ItemPrice.Create(Id, currencyCode, amount, isActive, validFrom);
        if (created.IsSuccess)
        {
            _prices.Add(created.Value);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetAvailability(DateTime? startsAt, DateTime? endsAt, string? channel)
    {
        var created = AvailabilityWindow.Create(Id, startsAt, endsAt, channel);
        if (created.IsSuccess)
        {
            _availability.Add(created.Value);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearAvailability() => _availability.Clear();

    public void ClearStats() => _stats.Clear();

    public void ClearPrices() => _prices.Clear();

    public void ApplySnapshot(CatalogItemSnapshot snapshot, long version)
    {
        Rarity = Enum.Parse<ItemRarity>(snapshot.Rarity);
        DisplayName = snapshot.DisplayName;
        Description = snapshot.Description;
        ModelCode = snapshot.ModelCode;
        Slot = snapshot.Slot;
        Tags = [.. snapshot.Tags];
        FitTags = [.. snapshot.FitTags];
        Status = ItemStatus.PUBLISHED;
        LastPublishedVersion = version;
        UpdatedAt = DateTime.UtcNow;

        _stats.Clear();
        foreach (CatalogItemStatSnapshot stat in snapshot.Stats)
        {
            var result = ItemStat.Create(Id, stat.Key, stat.Value);
            if (result.IsSuccess)
            {
                _stats.Add(result.Value);
            }
        }

        _prices.Clear();
        foreach (CatalogItemPriceSnapshot price in snapshot.Prices)
        {
            var result = ItemPrice.Create(Id, price.CurrencyCode, price.Amount, price.IsActive, price.ValidFrom);
            if (result.IsSuccess)
            {
                _prices.Add(result.Value);
            }
        }

        _availability.Clear();
        foreach (CatalogAvailabilitySnapshot window in snapshot.Availability)
        {
            var result = AvailabilityWindow.Create(Id, window.StartsAt, window.EndsAt, window.Channel);
            if (result.IsSuccess)
            {
                _availability.Add(result.Value);
            }
        }
    }


    public void ReplaceStats(IEnumerable<ItemStat> stats)
    {
        _stats.Clear();
        _stats.AddRange(stats);
    }

    public void ReplacePrices(IEnumerable<ItemPrice> prices)
    {
        _prices.Clear();
        _prices.AddRange(prices);
    }

    public void ReplaceAvailability(IEnumerable<AvailabilityWindow> availability)
    {
        _availability.Clear();
        _availability.AddRange(availability);
    }

    public static Item Restore(
        Guid id,
        string code,
        ItemKind kind,
        ItemRarity rarity,
        ItemStatus status,
        string displayName,
        string? description,
        string? modelCode,
        string? slot,
        string[] tags,
        string[] fitTags,
        long? lastPublishedVersion,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var item = new Item
        {
            Id = id,
            Code = code,
            Kind = kind,
            Rarity = rarity,
            Status = status,
            DisplayName = displayName,
            Description = description,
            ModelCode = modelCode,
            Slot = slot,
            Tags = tags,
            FitTags = fitTags,
            LastPublishedVersion = lastPublishedVersion,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

        return item;
    }

    public IReadOnlyList<string> Capabilities()
    {
        var capabilities = new List<string>(Tags);
        if (!string.IsNullOrWhiteSpace(ModelCode))
        {
            capabilities.Add($"{CatalogConstants.MODEL_FIT_PREFIX}{ModelCode}");
        }

        return capabilities;
    }

    private static Result<string, Error> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired("code");
        }

        string normalized = code.Trim().ToLowerInvariant();
        if (normalized.Length > CatalogConstants.CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("code", max: CatalogConstants.CODE_MAX_LENGTH);
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
        if (normalized.Length > CatalogConstants.DISPLAY_NAME_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("displayName", max: CatalogConstants.DISPLAY_NAME_MAX_LENGTH);
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
        if (normalized.Length > CatalogConstants.DESCRIPTION_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("description", max: CatalogConstants.DESCRIPTION_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string?, Error> NormalizeModelCode(ItemKind kind, string? modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode))
        {
            return kind == ItemKind.CAR ? CatalogErrors.ModelCodeRequired : (string?)null;
        }

        string normalized = modelCode.Trim().ToLowerInvariant();
        if (normalized.Length > CatalogConstants.MODEL_CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("modelCode", max: CatalogConstants.MODEL_CODE_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string?, Error> NormalizeSlot(string? slot)
    {
        if (string.IsNullOrWhiteSpace(slot))
        {
            return (string?)null;
        }

        string normalized = slot.Trim().ToLowerInvariant();
        if (normalized.Length > CatalogConstants.SLOT_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("slot", max: CatalogConstants.SLOT_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string[], Error> NormalizeTags(IReadOnlyList<string>? tags) =>
        NormalizeTagList("tags", tags, required: false);

    private static Result<string[], Error> NormalizeFitTags(ItemKind kind, IReadOnlyList<string>? fitTags) =>
        NormalizeTagList("fitTags", fitTags, required: kind == ItemKind.CAR_PART);

    private static Result<string[], Error> NormalizeTagList(
        string field,
        IReadOnlyList<string>? tags,
        bool required)
    {
        if (tags is null || tags.Count == 0)
        {
            if (required)
            {
                return CatalogErrors.FitTagsRequired;
            }

            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        foreach (string tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            string trimmed = tag.Trim().ToLowerInvariant();
            if (trimmed.Length > CatalogConstants.TAG_MAX_LENGTH)
            {
                return GeneralErrors.LengthIsInvalid(field, max: CatalogConstants.TAG_MAX_LENGTH);
            }

            if (!normalized.Contains(trimmed, StringComparer.Ordinal))
            {
                normalized.Add(trimmed);
            }
        }

        if (required && normalized.Count == 0)
        {
            return CatalogErrors.FitTagsRequired;
        }

        if (normalized.Count > CatalogConstants.MAX_TAGS)
        {
            return GeneralErrors.ValueIsInvalid(field);
        }

        return normalized.ToArray();
    }
}
