using InventoryService.Domain.Inventories;

namespace InventoryService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Inventory> InventoriesRead { get; }

    IQueryable<GrantReceipt> GrantReceiptsRead { get; }
}
