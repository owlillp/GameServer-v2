using InventoryService.Contracts.Requests;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Core.Services;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.ClanWars.Commands;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Messaging.Handlers;

public sealed class GrantSeasonRewardItemsCommandHandler(
    ILogger<GrantSeasonRewardItemsCommandHandler> logger,
    IInventoryGrantService grantService,
    ISeasonRewardResultPublisher publisher,
    ITransactionManager transactionManager)
{
    public async Task Handle(GrantSeasonRewardItemsCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Granting season reward {GrantId} of season {SeasonId} to account {AccountId}",
            command.GrantId,
            command.SeasonId,
            command.AccountId);

        var request = new GrantItemsRequest(
            [.. command.Items.Select(item => new GrantItemRequest(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))],
            "CLANWARS",
            command.GrantId,
            $"season:{command.SeasonId}:{command.AccountId}");

        var result = await grantService.ApplyAsync(
            new GrantItemsCommand(command.AccountId, request),
            cancellationToken);

        if (result.IsFailure && !result.Error.IsBusinessRejection())
        {
            throw result.Error.ToHandlerException();
        }

        if (result.IsFailure)
        {
            string reason = Describe(result.Error);

            logger.LogWarning(
                "Season reward grant failed for {GrantId}: {Error}",
                command.GrantId,
                reason);

            await publisher.PublishFailedAsync(
                command.GrantId,
                command.AccountId,
                reason,
                cancellationToken);
        }
        else
        {
            await publisher.PublishGrantedAsync(
                command.GrantId,
                command.AccountId,
                [.. command.Items.Select(item => item.ItemId)],
                cancellationToken);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure && saveResult.Error.IsUniqueViolation())
        {
            // A concurrent delivery of the same command already wrote the receipt and the result event.
            return;
        }

        saveResult.ThrowIfFailed();
    }

    private static string Describe(Error error) =>
        error.Messages.Count == 0
            ? "Season reward grant failed"
            : string.Join("; ", error.Messages.Select(message => message.Message));
}
