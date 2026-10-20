using System.Text.Json;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;
using Shared.IntegrationEvents.Wallets.Events;

namespace RealtimeService.Core.Messaging.Handlers;

public sealed class WalletCreditedNotificationHandler(IRealtimePublisher publisher)
{
    public Task Handle(WalletCreditedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"wallet.credited:{@event.OperationId}"),
                RealtimeNotificationTypes.WALLET_CREDITED,
                RealtimeAudiences.Player(@event.WalletId),
                @event.OccurredAt,
                Version: 1,
                RealtimeJson.ToElement(new WalletChangedPayload(
                    @event.WalletId,
                    @event.WalletId,
                    @event.Currency,
                    @event.Amount,
                    @event.Reason,
                    @event.OperationId))),
            cancellationToken);
}
