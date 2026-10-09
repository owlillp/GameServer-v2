using InventoryService.Contracts.Dtos;
using InventoryService.Contracts.Requests;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.GrantItems;

public sealed class GrantItemsHandler(
    IValidator<GrantItemsCommand> validator,
    IInventoryRepository repository,
    ITransactionManager transactionManager,
    IInventoryEventPublisher eventPublisher) : ICommandHandler<InventoryDto, GrantItemsCommand>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        GrantItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Guid operationId = command.Request.OperationId ?? Guid.CreateVersion7();

        bool alreadyProcessed = await repository.ReceiptExistsAsync(operationId, cancellationToken);
        if (alreadyProcessed)
        {
            Inventory? processed = await repository.FindAsync(command.AccountId, cancellationToken);
            return processed is not null
                ? InventoryMapper.ToDto(processed)
                : InventoryErrors.OperationAlreadyProcessed;
        }

        Inventory inventory = await repository.GetOrCreateAsync(command.AccountId, cancellationToken);

        var grantedIds = new List<Guid>();
        foreach (GrantItemRequest item in command.Request.Items)
        {
            var grantResult = inventory.GrantItem(
                item.ItemId,
                item.Code,
                item.Kind,
                item.Slot,
                command.Request.Source,
                item.Quantity);

            if (grantResult.IsFailure)
            {
                return grantResult.Error;
            }

            grantedIds.Add(item.ItemId);
        }

        var receiptResult = GrantReceipt.Create(
            command.AccountId,
            operationId,
            command.Request.Source,
            command.Request.Items.Count);

        if (receiptResult.IsFailure)
        {
            return receiptResult.Error;
        }

        await repository.AddReceiptAsync(receiptResult.Value, cancellationToken);

        await eventPublisher.PublishItemsGrantedAsync(
            command.AccountId,
            grantedIds,
            command.Request.Source,
            operationId);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return InventoryMapper.ToDto(inventory);
    }
}
