using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Versions.Queries.GetVersion;

public sealed record GetVersionQuery(long Number) : IQuery;
