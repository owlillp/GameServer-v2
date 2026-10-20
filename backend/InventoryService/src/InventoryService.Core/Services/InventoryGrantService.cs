using CSharpFunctionalExtensions;
using FluentValidation;
using InventoryService.Contracts.Dtos;
using InventoryService.Contracts.Requests;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Domain.Inventories;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Services;

public interface IInventoryGrantService
{
    Task<Result<InventoryDto, Error>> ApplyAsync(
        GrantItemsCommand command,
        CancellationToken cancellationToken);
}

public sealed class InventoryGrantService(
    IValidator<GrantItemsCommand> validator,
    IInventoryRepository repository,
    IInventoryEventPublisher eventPublisher) : IInventoryGrantService
{
    public async Task<Result<InventoryDto, Error>> ApplyAsync(
        GrantItemsCommand command,
        CancellationToken cancellationToken)
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

            if (string.Equals(item.Kind, InventoryConstants.CAR_KIND, StringComparison.OrdinalIgnoreCase))
            {
                for (int index = 0; index < item.Quantity; index++)
                {
                    var carResult = inventory.AddCar(item.ItemId, item.Code, color: null, plate: null);
                    if (carResult.IsFailure)
                    {
                        return carResult.Error;
                    }
                }
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

        return InventoryMapper.ToDto(inventory);
    }
}
