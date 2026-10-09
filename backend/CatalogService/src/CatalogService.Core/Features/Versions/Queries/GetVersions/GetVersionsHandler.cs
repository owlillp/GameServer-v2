using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Features.Versions.Models;
using CatalogService.Domain.Versions;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Versions.Queries.GetVersions;

public sealed class GetVersionsHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<CatalogVersionDto>, GetVersionsQuery>
{
    private const int MAX_VERSIONS = 100;

    public async Task<Result<IReadOnlyList<CatalogVersionDto>, Error>> Handle(
        GetVersionsQuery query,
        CancellationToken cancellationToken = default)
    {
        List<CatalogVersion> versions = await dbContext.VersionsRead
            .Include(version => version.Items)
            .OrderByDescending(version => version.Number)
            .Take(MAX_VERSIONS)
            .ToListAsync(cancellationToken);

        long? currentNumber = versions.Count > 0 ? versions[0].Number : null;

        return Result.Success<IReadOnlyList<CatalogVersionDto>, Error>(
            [.. versions.Select(version => VersionMapper.ToDto(version, version.Number == currentNumber))]);
    }
}
