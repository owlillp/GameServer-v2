using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Features.Versions.Models;
using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Versions.Queries.GetVersion;

public sealed class GetVersionHandler(IReadDbContext dbContext)
    : IQueryHandler<CatalogVersionDetailDto, GetVersionQuery>
{
    public async Task<Result<CatalogVersionDetailDto, Error>> Handle(
        GetVersionQuery query,
        CancellationToken cancellationToken = default)
    {
        CatalogVersion? version = await dbContext.VersionsRead
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Number == query.Number, cancellationToken);

        if (version is null)
        {
            return CatalogErrors.VersionNotFound;
        }

        long? currentNumber = await dbContext.VersionsRead
            .MaxAsync(entity => (long?)entity.Number, cancellationToken);

        return VersionMapper.ToDetailDto(version, version.Number == currentNumber);
    }
}
