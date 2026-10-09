using InventoryService.Contracts.Dtos;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.RevokeItems;

public sealed class RevokeItemsHandler(
    IValidator<RevokeItemsCommand> validator,
    IInventoryRepository repository,
    ITransactionManager transactionManager) : ICommandHandler<InventoryDto, RevokeItemsCommand>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        RevokeItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Inventory inventory = await repository.GetOrCreateAsync(command.AccountId, cancellationToken);

        foreach (Guid itemId in command.Request.ItemIds)
        {
            var revokeResult = inventory.RevokeItem(itemId);
            if (revokeResult.IsFailure)
            {
                return revokeResult.Error;
            }
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return InventoryMapper.ToDto(inventory);
    }
}
