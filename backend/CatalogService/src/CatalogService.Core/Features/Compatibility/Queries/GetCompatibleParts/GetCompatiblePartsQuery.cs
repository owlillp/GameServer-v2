using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Compatibility.Queries.GetCompatibleParts;

public sealed record GetCompatiblePartsQuery(Guid CarItemId, string? Slot) : IQuery;
