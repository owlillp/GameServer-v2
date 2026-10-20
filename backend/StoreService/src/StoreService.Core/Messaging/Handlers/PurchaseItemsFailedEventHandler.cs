using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Stores.Events;
using StoreService.Core.Abstractions;
using WalletService.Contracts.Communication;
using StoreService.Domain.Orders;

namespace StoreService.Core.Messaging.Handlers;

public sealed class PurchaseItemsFailedEventHandler(
    ILogger<PurchaseItemsFailedEventHandler> logger,
    IOrderRepository orderRepository,
    IWalletClient walletClient,
    ITransactionManager transactionManager)
{
    public async Task Handle(PurchaseItemsFailedEvent @event, CancellationToken cancellationToken)
    {
        var orderResult = await orderRepository.GetByIdAsync(@event.OrderId, cancellationToken);
        if (orderResult.IsFailure)
        {
            logger.LogWarning("Order {OrderId} was not found on grant failure", @event.OrderId);
            return;
        }

        Order order = orderResult.Value;
        if (order.IsTerminal)
        {
            return;
        }

        if (order.HoldId is not null)
        {
            var releaseResult = await walletClient.ReleaseAsync(order.HoldId.Value, cancellationToken);
            if (releaseResult.IsFailure)
            {
                logger.LogError(
                    "Wallet release failed for order {OrderId}: {Error}",
                    order.Id,
                    releaseResult.Error.ToString());

                throw new InvalidOperationException($"Wallet release failed for order {order.Id}");
            }
        }

        order.Fail(@event.Reason);
        await transactionManager.SaveChangesAsync(cancellationToken);
    }
}
