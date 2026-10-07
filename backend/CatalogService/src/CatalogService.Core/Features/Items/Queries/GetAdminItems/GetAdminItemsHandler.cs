using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Features.Items.Models;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Queries.GetAdminItems;

public sealed class GetAdminItemsHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<ItemDetailDto>, GetAdminItemsQuery>
{
    public async Task<Result<IReadOnlyList<ItemDetailDto>, Error>> Handle(
        GetAdminItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Item> items = dbContext.ItemsRead
            .AsNoTracking()
            .Include(item => item.Stats)
            .Include(item => item.Prices)
            .Include(item => item.Availability);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse(query.Status, ignoreCase: true, out ItemStatus status) || !Enum.IsDefined(status))
            {
                return GeneralErrors.ValueIsInvalid("status");
            }

            items = items.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            var kindResult = ItemKindParser.Parse(query.Kind);
            if (kindResult.IsFailure)
            {
                return kindResult.Error;
            }

            ItemKind kind = kindResult.Value;
            items = items.Where(item => item.Kind == kind);
        }

        List<Item> result = await items
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ItemDetailDto>, Error>([.. result.Select(ItemMapper.ToDetailDto)]);
    }
}
