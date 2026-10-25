using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Stores.Events;
using StoreService.Core.Abstractions;
using WalletService.Contracts.Communication;
using StoreService.Core.Messaging.Publishers;
using StoreService.Domain.Orders;

namespace StoreService.Core.Messaging.Handlers;

public sealed class PurchaseItemsGrantedEventHandler(
    ILogger<PurchaseItemsGrantedEventHandler> logger,
    IOrderRepository orderRepository,
    IWalletClient walletClient,
    IStoreEventPublisher eventPublisher,
    ITransactionManager transactionManager)
{
    public async Task Handle(PurchaseItemsGrantedEvent @event, CancellationToken cancellationToken)
    {
        var orderResult = await orderRepository.GetByIdAsync(@event.OrderId, cancellationToken);
        if (orderResult.IsFailure)
        {
            logger.LogWarning("Order {OrderId} was not found on grant confirmation", @event.OrderId);
            return;
        }

        Order order = orderResult.Value;
        if (order.Status == OrderStatus.COMPLETED)
        {
            return;
        }

        if (order.Status == OrderStatus.FAILED)
        {
            logger.LogCritical(
                "Order {OrderId} is already failed but items were granted; manual reconciliation is required",
                order.Id);

            return;
        }

        if (order.HoldId is null)
        {
            order.Fail("Wallet hold is missing");
            (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
            return;
        }

        var commitResult = await walletClient.CommitAsync(order.HoldId.Value, cancellationToken);
        if (commitResult.IsFailure)
        {
            logger.LogError(
                "Wallet commit failed for order {OrderId}: {Error}",
                order.Id,
                commitResult.Error.ToString());

            throw commitResult.Error.AsTransient().ToException();
        }

        if (order.Status == OrderStatus.RESERVED)
        {
            order.MarkGrantRequested();
        }

        var completeResult = order.Complete();
        if (completeResult.IsFailure)
        {
            logger.LogError("Order {OrderId} could not be completed: {Error}", order.Id, completeResult.Error.ToString());
            throw new InvalidOperationException($"Order {order.Id} is in an invalid state");
        }

        await eventPublisher.PublishPurchaseCompletedAsync(
            order.Id,
            order.AccountId,
            order.OfferId,
            order.Currency,
            order.TotalAmount,
            [.. order.Items.Select(item => item.CatalogItemId)],
            cancellationToken);

        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }
}
