using InventoryService.Contracts.Dtos;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.RemoveCar;

public sealed class RemoveCarHandler(
    IInventoryRepository repository,
    ITransactionManager transactionManager) : ICommandHandler<InventoryDto, RemoveCarCommand>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        RemoveCarCommand command,
        CancellationToken cancellationToken = default)
    {
        Inventory inventory = await repository.GetOrCreateAsync(command.AccountId, cancellationToken);

        var result = inventory.RemoveCar(command.CarId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return InventoryMapper.ToDto(inventory);
    }
}
