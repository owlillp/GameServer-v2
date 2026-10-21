using Shared.IntegrationEvents.Matchmaking.Events;

namespace InventoryService.Core.Messaging.Publishers;

public interface IMatchRewardResultPublisher
{
    Task PublishGrantedAsync(
        Guid grantId,
        Guid matchId,
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);

    Task PublishFailedAsync(
        Guid grantId,
        Guid matchId,
        Guid accountId,
        string reason,
        CancellationToken cancellationToken);
}

public sealed class MatchRewardResultPublisher(IOutboxService outbox) : IMatchRewardResultPublisher
{
    public Task PublishGrantedAsync(
        Guid grantId,
        Guid matchId,
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new MatchRewardGrantedEvent(
            grantId,
            matchId,
            accountId,
            itemIds,
            DateTime.UtcNow));

    public Task PublishFailedAsync(
        Guid grantId,
        Guid matchId,
        Guid accountId,
        string reason,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new MatchRewardGrantFailedEvent(
            grantId,
            matchId,
            accountId,
            reason,
            DateTime.UtcNow));
}
