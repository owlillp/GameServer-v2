using TemplateService.Domain;

namespace TemplateService.Core.Abstractions;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken);

    Task AddAsync(Item item, CancellationToken cancellationToken);

    void Remove(Item item);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
