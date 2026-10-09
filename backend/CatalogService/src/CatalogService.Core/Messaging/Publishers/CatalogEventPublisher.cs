using Shared.IntegrationEvents.Catalog.Events;

namespace CatalogService.Core.Messaging.Publishers;

public interface ICatalogEventPublisher
{
    Task PublishVersionPublishedAsync(
        Guid versionId,
        long version,
        int itemCount,
        string? note);
}

public sealed class CatalogEventPublisher(IOutboxService outbox) : ICatalogEventPublisher
{
    public Task PublishVersionPublishedAsync(
        Guid versionId,
        long version,
        int itemCount,
        string? note) =>
        outbox.PublishAsync(new CatalogVersionPublishedEvent(
            versionId,
            version,
            itemCount,
            note,
            DateTime.UtcNow));
}
