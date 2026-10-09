using InventoryService.Contracts.Dtos;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.InstallPart;

public sealed class InstallPartHandler(
    IInventoryRepository repository,
    ITransactionManager transactionManager) : ICommandHandler<InventoryDto, InstallPartCommand>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        InstallPartCommand command,
        CancellationToken cancellationToken = default)
    {
        Inventory inventory = await repository.GetOrCreateAsync(command.AccountId, cancellationToken);

        var result = inventory.InstallPart(command.CarId, command.Slot, command.PartItemId);
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
