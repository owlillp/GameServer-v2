using InventoryService.Domain.Inventories;

namespace InventoryService.Core.Abstractions;

public interface IInventoryRepository
{
    Task<Inventory> GetOrCreateAsync(Guid accountId, CancellationToken cancellationToken);

    Task<Inventory?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    Task AddAsync(Inventory inventory, CancellationToken cancellationToken);

    Task<bool> ReceiptExistsAsync(Guid operationId, CancellationToken cancellationToken);

    Task AddReceiptAsync(GrantReceipt receipt, CancellationToken cancellationToken);
}
