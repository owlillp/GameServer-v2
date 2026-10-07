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

namespace CatalogService.Core.Features.Items.Queries.GetItems;

public sealed class GetItemsHandler(
    IReadDbContext dbContext,
    HybridCache cache) : IQueryHandler<IReadOnlyList<ItemDto>, GetItemsQuery>
{
    private const int MAX_LIMIT = 500;

    public async Task<Result<IReadOnlyList<ItemDto>, Error>> Handle(
        GetItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        ItemKind? kind = null;
        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            var kindResult = ItemKindParser.Parse(query.Kind);
            if (kindResult.IsFailure)
            {
                return kindResult.Error;
            }

            kind = kindResult.Value;
        }

        int limit = Math.Clamp(query.Limit, 1, MAX_LIMIT);
        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var items = await cache.GetOrCreateAsync<IReadOnlyList<ItemDto>>(
            CatalogCacheKeys.List(query.Kind, search, limit),
            token => LoadAsync(kind, search, limit, token),
            options: null,
            tags: [CatalogCacheKeys.ListTag],
            cancellationToken);

        return Result.Success<IReadOnlyList<ItemDto>, Error>(items);
    }

    private async ValueTask<IReadOnlyList<ItemDto>> LoadAsync(
        ItemKind? kind,
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        IQueryable<Item> query = dbContext.ItemsRead
            .AsNoTracking()
            .Include(item => item.Prices)
            .Where(item => item.Status == ItemStatus.Published);

        if (kind is not null)
        {
            query = query.Where(item => item.Kind == kind);
        }

        if (search is not null)
        {
            query = query.Where(item => item.DisplayName.Contains(search));
        }

        List<Item> items = await query
            .OrderBy(item => item.DisplayName)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return [.. items.Select(ItemMapper.ToListDto)];
    }
}
