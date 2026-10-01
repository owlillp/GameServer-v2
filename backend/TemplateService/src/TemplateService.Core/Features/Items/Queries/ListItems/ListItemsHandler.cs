using TemplateService.Contracts.Items;
using TemplateService.Core.Abstractions;
using TemplateService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using Shared.SharedKernel.Responses;

namespace TemplateService.Core.Features.Items.Queries.ListItems;

public sealed class ListItemsHandler(IReadDbContext readDbContext)
    : IQueryHandler<PaginationResponse<ItemDto>, ListItemsQuery>
{
    private const int DEFAULT_PAGE_SIZE = 20;
    private const int MAX_PAGE_SIZE = 100;

    public async Task<Result<PaginationResponse<ItemDto>, Error>> Handle(
        ListItemsQuery query,
        CancellationToken cancellationToken = new ())
    {
        int page = Math.Max(1, query.Page);
        int pageSize = query.PageSize <= 0
            ? DEFAULT_PAGE_SIZE
            : Math.Min(query.PageSize, MAX_PAGE_SIZE);

        IQueryable<Item> itemsQuery = readDbContext.ItemsRead;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToUpperInvariant();

            itemsQuery = itemsQuery.Where(item => item.NormalizedName.Contains(term));
        }

        int totalCount = await itemsQuery.CountAsync(cancellationToken);

        List<ItemDto> records = await itemsQuery
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.OwnerId,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

        return new PaginationResponse<ItemDto>(records, totalCount, page, pageSize, totalPages);
    }
}
