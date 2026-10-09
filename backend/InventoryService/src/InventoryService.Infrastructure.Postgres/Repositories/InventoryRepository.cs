using InventoryService.Core.Abstractions;
using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Postgres.Repositories;

public sealed class InventoryRepository(InventoryServiceDbContext dbContext) : IInventoryRepository
{
    public async Task<Inventory> GetOrCreateAsync(Guid accountId, CancellationToken cancellationToken)
    {
        Inventory? existing = await WithChildren(dbContext.Inventories)
            .FirstOrDefaultAsync(inventory => inventory.AccountId == accountId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var createdResult = Inventory.Create(accountId);
        Inventory created = createdResult.Value;

        dbContext.Inventories.Add(created);
        await dbContext.SaveChangesAsync(cancellationToken);

        return created;
    }

    public Task<Inventory?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        WithChildren(dbContext.Inventories)
            .FirstOrDefaultAsync(inventory => inventory.AccountId == accountId, cancellationToken);

    public Task AddAsync(Inventory inventory, CancellationToken cancellationToken)
    {
        dbContext.Inventories.Add(inventory);
        return Task.CompletedTask;
    }

    public Task<bool> ReceiptExistsAsync(Guid operationId, CancellationToken cancellationToken) =>
        dbContext.GrantReceipts.AnyAsync(receipt => receipt.OperationId == operationId, cancellationToken);

    public Task AddReceiptAsync(GrantReceipt receipt, CancellationToken cancellationToken)
    {
        dbContext.GrantReceipts.Add(receipt);
        return Task.CompletedTask;
    }

    private static IQueryable<Inventory> WithChildren(IQueryable<Inventory> query) =>
        query
            .Include(inventory => inventory.Items)
            .Include(inventory => inventory.Outfit)
            .Include(inventory => inventory.Cars).ThenInclude(car => car.Installations)
            .AsSplitQuery();
}
