namespace InventoryService.Domain.Inventories;

public sealed class OwnedCar
{
    private readonly List<CarInstallation> _installations = [];

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid CarItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public string? Color { get; private set; }

    public string? Plate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<CarInstallation> Installations => _installations;

    // EF Core
    private OwnedCar() { }

    private OwnedCar(Guid accountId, Guid carItemId, string code, string? color, string? plate)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        CarItemId = carItemId;
        Code = code;
        Color = color;
        Plate = plate;
        CreatedAt = DateTime.UtcNow;
    }

    internal static OwnedCar Create(Guid accountId, Guid carItemId, string code, string? color, string? plate) =>
        new(accountId, carItemId, code, color, plate);

    internal void SetInstallation(string slot, Guid partItemId)
    {
        CarInstallation? existing = _installations
            .FirstOrDefault(installation => string.Equals(installation.Slot, slot, StringComparison.Ordinal));

        if (existing is not null)
        {
            existing.SetPart(partItemId);
            return;
        }

        _installations.Add(CarInstallation.Create(Id, slot, partItemId));
    }

    internal bool RemoveInstallation(string slot)
    {
        CarInstallation? existing = _installations
            .FirstOrDefault(installation => string.Equals(installation.Slot, slot, StringComparison.Ordinal));

        return existing is not null && _installations.Remove(existing);
    }
}
