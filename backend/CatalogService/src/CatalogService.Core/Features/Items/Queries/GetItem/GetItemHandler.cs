using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Cache;
using CatalogService.Core.Features.Items.Models;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Queries.GetItem;

public sealed class GetItemHandler(
    IReadDbContext dbContext,
    HybridCache cache) : IQueryHandler<ItemDetailDto, GetItemQuery>
{
    public async Task<Result<ItemDetailDto, Error>> Handle(
        GetItemQuery query,
        CancellationToken cancellationToken = default)
    {
        ItemDetailDto? item = await cache.GetOrCreateAsync<ItemDetailDto?>(
            CatalogCacheKeys.Item(query.ItemId),
            token => LoadAsync(query.ItemId, token),
            options: null,
            tags: [CatalogCacheKeys.ListTag],
            cancellationToken);

        return item is null
            ? CatalogErrors.ItemNotFound(query.ItemId)
            : item;
    }

    private async ValueTask<ItemDetailDto?> LoadAsync(Guid itemId, CancellationToken cancellationToken)
    {
        Item? item = await dbContext.ItemsRead
            .AsNoTracking()
            .Include(entity => entity.Stats)
            .Include(entity => entity.Prices)
            .Include(entity => entity.Availability)
            .FirstOrDefaultAsync(
                entity => entity.Id == itemId && entity.Status == ItemStatus.Published,
                cancellationToken);

        return item is null
            ? null
            : ItemMapper.ToDetailDto(item);
    }
}
