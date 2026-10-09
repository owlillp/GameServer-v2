using InventoryService.Contracts.Dtos;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Queries.GetMyInventory;

public sealed class GetMyInventoryHandler(
    IReadDbContext dbContext,
    UserScopedData userScope) : IQueryHandler<InventoryDto, GetMyInventoryQuery>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        GetMyInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        Guid? accountId = userScope.Id;
        if (accountId is null)
        {
            return Error.Authentication("authentication.required", "User identity is not available");
        }

        Inventory? inventory = await dbContext.InventoriesRead
            .AsNoTracking()
            .Include(entity => entity.Items)
            .Include(entity => entity.Outfit)
            .Include(entity => entity.Cars).ThenInclude(car => car.Installations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(entity => entity.AccountId == accountId.Value, cancellationToken);

        if (inventory is null)
        {
            return GeneralErrors.NotFound();
        }

        return InventoryMapper.ToDto(inventory);
    }
}
