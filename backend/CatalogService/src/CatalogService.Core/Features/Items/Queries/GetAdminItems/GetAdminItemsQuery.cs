using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Queries.GetAdminItems;

public sealed record GetAdminItemsQuery(string? Status, string? Kind) : IQuery;
