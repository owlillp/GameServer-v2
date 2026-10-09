using CSharpFunctionalExtensions;
using CatalogService.Domain.Items;
using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Versions;

public sealed class CatalogVersion
{
    private readonly List<CatalogVersionItem> _items = [];

    public Guid Id { get; private set; }

    public long Number { get; private set; }

    public string? Note { get; private set; }

    public Guid? PublishedBy { get; private set; }

    public DateTime PublishedAt { get; private set; }

    public IReadOnlyList<CatalogVersionItem> Items => _items;

    // EF Core
    private CatalogVersion() { }

    private CatalogVersion(Guid id, long number, string? note, Guid? publishedBy)
    {
        Id = id;
        Number = number;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        PublishedBy = publishedBy;
        PublishedAt = DateTime.UtcNow;
    }

    public static Result<CatalogVersion, Error> Create(long number, string? note, Guid? publishedBy)
    {
        if (number <= 0)
        {
            return GeneralErrors.ValueIsInvalid("version");
        }

        return new CatalogVersion(Guid.CreateVersion7(), number, note, publishedBy);
    }

    public void AddItem(Item item, string snapshot)
    {
        _items.Add(CatalogVersionItem.Create(Id, item.Id, item.Code, item.Kind, snapshot));
    }
}
