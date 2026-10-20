using CSharpFunctionalExtensions;
using InventoryService.Contracts.Dtos;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Services;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.GrantItems;

public sealed class GrantItemsHandler(
    IInventoryGrantService grantService,
    ITransactionManager transactionManager) : ICommandHandler<InventoryDto, GrantItemsCommand>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        GrantItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        var grantResult = await grantService.ApplyAsync(command, cancellationToken);
        if (grantResult.IsFailure)
        {
            return grantResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : grantResult.Value;
    }
}
