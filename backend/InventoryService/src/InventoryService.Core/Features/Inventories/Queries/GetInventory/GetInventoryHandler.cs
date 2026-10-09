using InventoryService.Contracts.Dtos;
using InventoryService.Core.Abstractions;
using InventoryService.Core.Features.Inventories.Models;
using InventoryService.Domain.Inventories;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Queries.GetInventory;

public sealed class GetInventoryHandler(IReadDbContext dbContext)
    : IQueryHandler<InventoryDto, GetInventoryQuery>
{
    public async Task<Result<InventoryDto, Error>> Handle(
        GetInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        Inventory? inventory = await dbContext.InventoriesRead
            .AsNoTracking()
            .Include(entity => entity.Items)
            .Include(entity => entity.Outfit)
            .Include(entity => entity.Cars).ThenInclude(car => car.Installations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(entity => entity.AccountId == query.AccountId, cancellationToken);

        if (inventory is null)
        {
            return GeneralErrors.NotFound();
        }

        return InventoryMapper.ToDto(inventory);
    }
}
