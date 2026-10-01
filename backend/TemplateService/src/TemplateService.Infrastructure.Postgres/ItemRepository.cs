using TemplateService.Core.Abstractions;
using TemplateService.Domain;
using Microsoft.EntityFrameworkCore;

namespace TemplateService.Infrastructure.Postgres;

public sealed class ItemRepository(TemplateServiceDbContext dbContext) : IItemRepository
{
    public Task<Item?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken) =>
        dbContext.Items.FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public Task AddAsync(Item item, CancellationToken cancellationToken)
    {
        dbContext.Items.Add(item);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Item item) => dbContext.Items.Remove(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
