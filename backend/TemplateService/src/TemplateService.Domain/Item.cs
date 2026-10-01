namespace TemplateService.Domain;

// Пример доменной сущности. Замени Item на бизнес-сущность нового сервиса.
public sealed class Item
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // Верхний регистр — для регистронезависимого поиска.
    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid OwnerId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private Item() { }

    public Item(string name, string? description, Guid ownerId)
    {
        Id = Guid.CreateVersion7();
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        Description = NormalizeDescription(description);
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        Description = NormalizeDescription(description);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
