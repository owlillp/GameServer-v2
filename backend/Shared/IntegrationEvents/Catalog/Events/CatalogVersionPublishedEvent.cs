namespace Shared.IntegrationEvents.Catalog.Events;

public record CatalogVersionPublishedEvent(
    Guid VersionId,
    long Version,
    int ItemCount,
    string? Note,
    DateTime OccurredAt);
