using InventoryService.Contracts.Requests;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Core.Services;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Events.Commands;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Messaging.Handlers;

public sealed class GrantEventRewardCommandHandler(
    ILogger<GrantEventRewardCommandHandler> logger,
    IInventoryGrantService grantService,
    IEventRewardResultPublisher publisher,
    ITransactionManager transactionManager)
{
    public async Task Handle(GrantEventRewardCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Granting event reward {RewardId} for claim {ClaimId} to account {AccountId}",
            command.RewardId,
            command.ClaimId,
            command.AccountId);

        var request = new GrantItemsRequest(
            [.. command.Items.Select(item => new GrantItemRequest(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))],
            "EVENT",
            command.ClaimId,
            $"{command.EventId}:{command.RewardId}");

        var result = await grantService.ApplyAsync(
            new GrantItemsCommand(command.AccountId, request),
            cancellationToken);

        if (result.IsFailure)
        {
            string reason = Describe(result.Error);

            logger.LogWarning(
                "Event reward grant failed for claim {ClaimId}: {Error}",
                command.ClaimId,
                reason);

            await publisher.PublishFailedAsync(
                command.ClaimId,
                command.EventId,
                command.RewardId,
                command.AccountId,
                reason,
                cancellationToken);
        }
        else
        {
            await publisher.PublishGrantedAsync(
                command.ClaimId,
                command.EventId,
                command.RewardId,
                command.AccountId,
                [.. command.Items.Select(item => item.ItemId)],
                cancellationToken);
        }

        await transactionManager.SaveChangesAsync(cancellationToken);
    }

    private static string Describe(Error error) =>
        error.Messages.Count == 0
            ? "Event reward grant failed"
            : string.Join("; ", error.Messages.Select(message => message.Message));
}
