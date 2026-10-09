using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Queries.GetItem;

public sealed record GetItemQuery(Guid ItemId) : IQuery;
