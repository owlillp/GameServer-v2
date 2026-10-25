using Shared.IntegrationEvents.ClanWars.Events;

namespace InventoryService.Core.Messaging.Publishers;

public interface ISeasonRewardResultPublisher
{
    Task PublishGrantedAsync(Guid grantId, Guid accountId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken);

    Task PublishFailedAsync(Guid grantId, Guid accountId, string reason, CancellationToken cancellationToken);
}

public sealed class SeasonRewardResultPublisher(IOutboxService outbox) : ISeasonRewardResultPublisher
{
    public Task PublishGrantedAsync(Guid grantId, Guid accountId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new SeasonRewardItemsGrantedEvent(grantId, accountId, itemIds, DateTime.UtcNow));

    public Task PublishFailedAsync(Guid grantId, Guid accountId, string reason, CancellationToken cancellationToken) =>
        outbox.PublishAsync(new SeasonRewardItemsFailedEvent(grantId, accountId, reason, DateTime.UtcNow));
}
