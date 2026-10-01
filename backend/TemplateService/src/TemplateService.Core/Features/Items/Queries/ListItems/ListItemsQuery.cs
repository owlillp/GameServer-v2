using Shared.Core.Abstractions;

namespace TemplateService.Core.Features.Items.Queries.ListItems;

public sealed record ListItemsQuery(int Page, int PageSize, string? Search) : IQuery;
