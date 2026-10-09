using CatalogService.Domain.Items;

namespace CatalogService.Domain.Versions;

public sealed class CatalogVersionItem
{
    public Guid Id { get; private set; }

    public Guid VersionId { get; private set; }

    public Guid ItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public ItemKind Kind { get; private set; }

    public string Snapshot { get; private set; } = null!;

    // EF Core
    private CatalogVersionItem() { }

    private CatalogVersionItem(Guid versionId, Guid itemId, string code, ItemKind kind, string snapshot)
    {
        Id = Guid.CreateVersion7();
        VersionId = versionId;
        ItemId = itemId;
        Code = code;
        Kind = kind;
        Snapshot = snapshot;
    }

    internal static CatalogVersionItem Create(Guid versionId, Guid itemId, string code, ItemKind kind, string snapshot) =>
        new(versionId, itemId, code, kind, snapshot);
}
