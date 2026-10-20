using Shared.IntegrationEvents.Stores.Events;

namespace StoreService.Core.Messaging.Publishers;

public interface IStoreEventPublisher
{
    Task PublishPurchaseCompletedAsync(
        Guid orderId,
        Guid accountId,
        Guid offerId,
        string currency,
        long totalAmount,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);
}

public sealed class StoreEventPublisher(IOutboxService outbox) : IStoreEventPublisher
{
    public Task PublishPurchaseCompletedAsync(
        Guid orderId,
        Guid accountId,
        Guid offerId,
        string currency,
        long totalAmount,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new PurchaseCompletedEvent(
            orderId,
            accountId,
            offerId,
            currency,
            totalAmount,
            itemIds,
            DateTime.UtcNow));
}
