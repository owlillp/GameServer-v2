using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Queries.GetItems;

public sealed record GetItemsQuery(string? Kind, string? Search, int Limit = 100) : IQuery;
