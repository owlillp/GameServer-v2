using InventoryService.Contracts.Dtos;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.AddCar;

public sealed class AddCarHandler(
    IInventoryRepository repository,
    ITransactionManager transactionManager) : ICommandHandler<InventoryDto, AddCarCommand>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        AddCarCommand command,
        CancellationToken cancellationToken = default)
    {
        Inventory inventory = await repository.GetOrCreateAsync(command.AccountId, cancellationToken);

        var result = inventory.AddCar(
            command.Request.CarItemId,
            command.Request.Code,
            command.Request.Color,
            command.Request.Plate);

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
