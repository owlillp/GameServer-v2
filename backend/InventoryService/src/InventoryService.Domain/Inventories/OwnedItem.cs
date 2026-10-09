namespace InventoryService.Domain.Inventories;

public sealed class OwnedItem
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid ItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string? Slot { get; private set; }

    public string Source { get; private set; } = null!;

    public int Quantity { get; private set; }

    public DateTime AcquiredAt { get; private set; }

    // EF Core
    private OwnedItem() { }

    private OwnedItem(
        Guid accountId,
        Guid itemId,
        string code,
        string kind,
        string? slot,
        string source,
        int quantity)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        ItemId = itemId;
        Code = code;
        Kind = kind;
        Slot = slot;
        Source = source;
        Quantity = quantity;
        AcquiredAt = DateTime.UtcNow;
    }

    internal static OwnedItem Create(
        Guid accountId,
        Guid itemId,
        string code,
        string kind,
        string? slot,
        string source,
        int quantity) =>
        new(accountId, itemId, code, kind, slot, source, quantity);

    internal void AddQuantity(int quantity) => Quantity += quantity;
}
