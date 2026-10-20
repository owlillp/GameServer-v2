using InventoryService.Contracts.Requests;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Messaging;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Core.Services;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Stores.Commands;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Messaging.Handlers;

public sealed class GrantPurchaseItemsCommandHandler(
    ILogger<GrantPurchaseItemsCommandHandler> logger,
    IInventoryGrantService grantService,
    IStorePurchaseResultPublisher publisher,
    ITransactionManager transactionManager)
{
    public async Task Handle(GrantPurchaseItemsCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Granting items for store order {OrderId} to account {AccountId}",
            command.OrderId,
            command.AccountId);

        var request = new GrantItemsRequest(
            [.. command.Items.Select(item => new GrantItemRequest(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))],
            "PURCHASE",
            command.OrderId,
            command.OrderId.ToString());

        var result = await grantService.ApplyAsync(
            new GrantItemsCommand(command.AccountId, request),
            cancellationToken);

        if (result.IsFailure)
        {
            string reason = Describe(result.Error);

            logger.LogWarning(
                "Item grant failed for store order {OrderId}: {Error}",
                command.OrderId,
                reason);

            await publisher.PublishFailedAsync(
                command.OrderId,
                command.AccountId,
                reason,
                cancellationToken);
        }
        else
        {
            await publisher.PublishGrantedAsync(
                command.OrderId,
                command.AccountId,
                [.. command.Items.Select(item => item.ItemId)],
                cancellationToken);
        }

        await transactionManager.SaveChangesAsync(cancellationToken);
    }

    private static string Describe(Error error) =>
        error.Messages.Count == 0
            ? "Item grant failed"
            : string.Join("; ", error.Messages.Select(message => message.Message));
}
