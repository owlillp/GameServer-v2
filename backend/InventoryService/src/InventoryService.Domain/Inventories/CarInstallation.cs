namespace InventoryService.Domain.Inventories;

public sealed class CarInstallation
{
    public Guid Id { get; private set; }

    public Guid CarId { get; private set; }

    public string Slot { get; private set; } = null!;

    public Guid PartItemId { get; private set; }

    // EF Core
    private CarInstallation() { }

    private CarInstallation(Guid carId, string slot, Guid partItemId)
    {
        Id = Guid.CreateVersion7();
        CarId = carId;
        Slot = slot;
        PartItemId = partItemId;
    }

    internal static CarInstallation Create(Guid carId, string slot, Guid partItemId) =>
        new(carId, slot, partItemId);

    internal void SetPart(Guid partItemId) => PartItemId = partItemId;
}
