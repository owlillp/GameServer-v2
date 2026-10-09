using Shared.IntegrationEvents.Inventories.Events;

namespace InventoryService.Core.Messaging.Publishers;

public interface IInventoryEventPublisher
{
    Task PublishItemsGrantedAsync(
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        string source,
        Guid operationId);
}

public sealed class InventoryEventPublisher(IOutboxService outbox) : IInventoryEventPublisher
{
    public Task PublishItemsGrantedAsync(
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        string source,
        Guid operationId) =>
        outbox.PublishAsync(new ItemsGrantedEvent(
            accountId,
            itemIds,
            source,
            operationId,
            DateTime.UtcNow));
}
