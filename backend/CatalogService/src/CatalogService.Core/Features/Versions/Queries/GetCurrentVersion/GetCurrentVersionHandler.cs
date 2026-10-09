using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Cache;
using CatalogService.Core.Features.Versions.Models;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Versions.Queries.GetCurrentVersion;

public sealed class GetCurrentVersionHandler(
    ICatalogVersionRepository versionRepository,
    HybridCache cache) : IQueryHandler<CatalogVersionDto, GetCurrentVersionQuery>
{
    public async Task<Result<CatalogVersionDto, Error>> Handle(
        GetCurrentVersionQuery query,
        CancellationToken cancellationToken = default)
    {
        var latestResult = await versionRepository.GetLatestNumberAsync(cancellationToken);
        if (latestResult.IsFailure)
        {
            return latestResult.Error;
        }

        if (latestResult.Value is not { } number)
        {
            return CatalogErrors.NoPublishedVersion;
        }

        CatalogVersionDto? version = await cache.GetOrCreateAsync<CatalogVersionDto?>(
            $"catalog:version:{number}",
            token => LoadAsync(number, token),
            options: null,
            tags: [CatalogCacheKeys.VersionTag],
            cancellationToken);

        return version is null
            ? CatalogErrors.VersionNotFound
            : version;
    }

    private async ValueTask<CatalogVersionDto?> LoadAsync(long number, CancellationToken cancellationToken)
    {
        var versionResult = await versionRepository.GetByNumberAsync(number, cancellationToken);
        return versionResult.IsFailure
            ? null
            : VersionMapper.ToDto(versionResult.Value, isCurrent: true);
    }
}
