namespace InventoryService.Domain.Inventories;

public sealed class OutfitEntry
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Slot { get; private set; } = null!;

    public Guid ItemId { get; private set; }

    // EF Core
    private OutfitEntry() { }

    private OutfitEntry(Guid accountId, string slot, Guid itemId)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        Slot = slot;
        ItemId = itemId;
    }

    internal static OutfitEntry Create(Guid accountId, string slot, Guid itemId) =>
        new(accountId, slot, itemId);

    internal void SetItem(Guid itemId) => ItemId = itemId;
}
