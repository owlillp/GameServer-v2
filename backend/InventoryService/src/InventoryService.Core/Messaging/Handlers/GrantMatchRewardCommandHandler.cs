using InventoryService.Contracts.Requests;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Core.Services;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Messaging.Handlers;

public sealed class GrantMatchRewardCommandHandler(
    ILogger<GrantMatchRewardCommandHandler> logger,
    IInventoryGrantService grantService,
    IMatchRewardResultPublisher publisher,
    ITransactionManager transactionManager)
{
    public async Task Handle(GrantMatchRewardCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Granting match reward {GrantId} for match {MatchId} to account {AccountId}",
            command.GrantId,
            command.MatchId,
            command.AccountId);

        var request = new GrantItemsRequest(
            [.. command.Items.Select(item => new GrantItemRequest(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))],
            "MATCH",
            command.GrantId,
            $"{command.MatchId}:{command.AccountId}");

        var result = await grantService.ApplyAsync(
            new GrantItemsCommand(command.AccountId, request),
            cancellationToken);

        if (result.IsFailure)
        {
            string reason = Describe(result.Error);

            logger.LogWarning(
                "Match reward grant failed for {GrantId}: {Error}",
                command.GrantId,
                reason);

            await publisher.PublishFailedAsync(
                command.GrantId,
                command.MatchId,
                command.AccountId,
                reason,
                cancellationToken);
        }
        else
        {
            await publisher.PublishGrantedAsync(
                command.GrantId,
                command.MatchId,
                command.AccountId,
                [.. command.Items.Select(item => item.ItemId)],
                cancellationToken);
        }

        await transactionManager.SaveChangesAsync(cancellationToken);
    }

    private static string Describe(Error error) =>
        error.Messages.Count == 0
            ? "Match reward grant failed"
            : string.Join("; ", error.Messages.Select(message => message.Message));
}
