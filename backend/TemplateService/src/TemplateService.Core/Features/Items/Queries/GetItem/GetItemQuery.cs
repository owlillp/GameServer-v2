using Shared.Core.Abstractions;

namespace TemplateService.Core.Features.Items.Queries.GetItem;

public sealed record GetItemQuery(Guid ItemId) : IQuery;
