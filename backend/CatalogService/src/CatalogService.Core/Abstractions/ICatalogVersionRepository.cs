using CatalogService.Domain.Versions;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Abstractions;

public interface ICatalogVersionRepository
{
    Task<Result<CatalogVersion, Error>> GetByNumberAsync(long number, CancellationToken cancellationToken);

    Task<Result<long?, Error>> GetLatestNumberAsync(CancellationToken cancellationToken);

    Task<Result<Guid, Error>> AddAsync(CatalogVersion version, CancellationToken cancellationToken);
}
