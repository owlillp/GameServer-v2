using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Items;

public sealed class ItemStat
{
    public Guid Id { get; private set; }

    public Guid ItemId { get; private set; }

    public string Key { get; private set; } = null!;

    public decimal Value { get; private set; }

    // EF Core
    private ItemStat() { }

    private ItemStat(Guid itemId, string key, decimal value)
    {
        Id = Guid.CreateVersion7();
        ItemId = itemId;
        Key = key;
        Value = value;
    }

    internal static Result<ItemStat, Error> Create(Guid itemId, string key, decimal value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return GeneralErrors.ValueIsRequired("stat.key");
        }

        if (key.Length > CatalogConstants.STAT_KEY_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("stat.key", max: CatalogConstants.STAT_KEY_MAX_LENGTH);
        }

        return new ItemStat(itemId, key.Trim(), value);
    }
}
