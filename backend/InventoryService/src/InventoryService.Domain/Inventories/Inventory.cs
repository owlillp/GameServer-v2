using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace InventoryService.Domain.Inventories;

public sealed class Inventory
{
    private readonly List<OwnedItem> _items = [];
    private readonly List<OutfitEntry> _outfit = [];
    private readonly List<OwnedCar> _cars = [];

    public Guid AccountId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<OwnedItem> Items => _items;

    public IReadOnlyList<OutfitEntry> Outfit => _outfit;

    public IReadOnlyList<OwnedCar> Cars => _cars;

    // EF Core
    private Inventory() { }

    private Inventory(Guid accountId)
    {
        AccountId = accountId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Result<Inventory, Error> Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        return new Inventory(accountId);
    }

    public Result<OwnedItem, Error> GrantItem(
        Guid itemId,
        string code,
        string kind,
        string? slot,
        string source,
        int quantity)
    {
        if (itemId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("itemId");
        }

        if (quantity <= 0)
        {
            return InventoryErrors.QuantityInvalid;
        }

        string normalizedCode = code.Trim().ToLowerInvariant();
        string normalizedKind = kind.Trim();
        string? normalizedSlot = NormalizeSlot(slot);
        string normalizedSource = source.Trim();

        OwnedItem? existing = _items.FirstOrDefault(item => item.ItemId == itemId);
        if (existing is not null)
        {
            existing.AddQuantity(quantity);
            Touch();
            return existing;
        }

        var created = OwnedItem.Create(AccountId, itemId, normalizedCode, normalizedKind, normalizedSlot, normalizedSource, quantity);
        _items.Add(created);
        Touch();
        return created;
    }

    public UnitResult<Error> RevokeItem(Guid itemId)
    {
        OwnedItem? existing = _items.FirstOrDefault(item => item.ItemId == itemId);
        if (existing is null)
        {
            return InventoryErrors.ItemNotOwned(itemId);
        }

        _items.Remove(existing);
        _outfit.RemoveAll(entry => entry.ItemId == itemId);
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> EquipOutfit(string slot, Guid itemId)
    {
        string? normalizedSlot = NormalizeSlot(slot);
        if (normalizedSlot is null)
        {
            return InventoryErrors.OutfitSlotRequired;
        }

        if (!OwnsItem(itemId))
        {
            return InventoryErrors.ItemNotOwned(itemId);
        }

        OutfitEntry? existing = _outfit
            .FirstOrDefault(entry => string.Equals(entry.Slot, normalizedSlot, StringComparison.Ordinal));

        if (existing is not null)
        {
            existing.SetItem(itemId);
        }
        else
        {
            _outfit.Add(OutfitEntry.Create(AccountId, normalizedSlot, itemId));
        }

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UnequipOutfit(string slot)
    {
        string? normalizedSlot = NormalizeSlot(slot);
        if (normalizedSlot is null)
        {
            return InventoryErrors.OutfitSlotRequired;
        }

        _outfit.RemoveAll(entry => string.Equals(entry.Slot, normalizedSlot, StringComparison.Ordinal));
        Touch();
        return UnitResult.Success<Error>();
    }

    public Result<OwnedCar, Error> AddCar(Guid carItemId, string code, string? color, string? plate)
    {
        if (!OwnsItem(carItemId))
        {
            return InventoryErrors.ItemNotOwned(carItemId);
        }

        var car = OwnedCar.Create(AccountId, carItemId, code.Trim().ToLowerInvariant(), Normalize(color), Normalize(plate));
        _cars.Add(car);
        Touch();
        return car;
    }

    public UnitResult<Error> RemoveCar(Guid carId)
    {
        OwnedCar? car = _cars.FirstOrDefault(entity => entity.Id == carId);
        if (car is null)
        {
            return InventoryErrors.CarNotFound(carId);
        }

        _cars.Remove(car);
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> InstallPart(Guid carId, string slot, Guid partItemId)
    {
        OwnedCar? car = _cars.FirstOrDefault(entity => entity.Id == carId);
        if (car is null)
        {
            return InventoryErrors.CarNotFound(carId);
        }

        string? normalizedSlot = NormalizeSlot(slot);
        if (normalizedSlot is null)
        {
            return GeneralErrors.ValueIsRequired("slot");
        }

        if (!OwnsItem(partItemId))
        {
            return InventoryErrors.ItemNotOwned(partItemId);
        }

        car.SetInstallation(normalizedSlot, partItemId);
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RemovePart(Guid carId, string slot)
    {
        OwnedCar? car = _cars.FirstOrDefault(entity => entity.Id == carId);
        if (car is null)
        {
            return InventoryErrors.CarNotFound(carId);
        }

        string? normalizedSlot = NormalizeSlot(slot);
        if (normalizedSlot is null)
        {
            return GeneralErrors.ValueIsRequired("slot");
        }

        car.RemoveInstallation(normalizedSlot);
        Touch();
        return UnitResult.Success<Error>();
    }

    private bool OwnsItem(Guid itemId) => _items.Any(item => item.ItemId == itemId);

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static string? NormalizeSlot(string? slot) =>
        string.IsNullOrWhiteSpace(slot) ? null : slot.Trim().ToLowerInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
