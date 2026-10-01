using TemplateService.Contracts.Items;
using TemplateService.Core.Abstractions;
using TemplateService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace TemplateService.Core.Features.Items.Queries.GetItem;

public sealed class GetItemHandler(IReadDbContext readDbContext)
    : IQueryHandler<ItemDto, GetItemQuery>
{
    public async Task<Result<ItemDto, Error>> Handle(
        GetItemQuery query,
        CancellationToken cancellationToken = new ())
    {
        ItemDto? item = await readDbContext.ItemsRead
            .Where(item => item.Id == query.ItemId)
            .Select(item => new ItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.OwnerId,
                item.CreatedAt,
                item.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return GeneralErrors.NotFound(query.ItemId, "item");
        }

        return item;
    }
}
