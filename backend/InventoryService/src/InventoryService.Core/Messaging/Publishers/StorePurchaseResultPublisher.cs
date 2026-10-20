using Shared.IntegrationEvents.Stores.Events;

namespace InventoryService.Core.Messaging.Publishers;

public interface IStorePurchaseResultPublisher
{
    Task PublishGrantedAsync(
        Guid orderId,
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);

    Task PublishFailedAsync(
        Guid orderId,
        Guid accountId,
        string reason,
        CancellationToken cancellationToken);
}

public sealed class StorePurchaseResultPublisher(IOutboxService outbox) : IStorePurchaseResultPublisher
{
    public Task PublishGrantedAsync(
        Guid orderId,
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new PurchaseItemsGrantedEvent(
            orderId,
            accountId,
            itemIds,
            DateTime.UtcNow));

    public Task PublishFailedAsync(
        Guid orderId,
        Guid accountId,
        string reason,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new PurchaseItemsFailedEvent(
            orderId,
            accountId,
            reason,
            DateTime.UtcNow));
}
