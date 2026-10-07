using CatalogService.Core.Abstractions;
using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;

namespace CatalogService.Infrastructure.Postgres.Repositories;

public sealed class CatalogVersionRepository(
    ILogger<CatalogVersionRepository> logger,
    CatalogServiceDbContext dbContext) : ICatalogVersionRepository
{
    public async Task<Result<CatalogVersion, Error>> GetByNumberAsync(long number, CancellationToken cancellationToken)
    {
        try
        {
            CatalogVersion? version = await dbContext.CatalogVersions
                .Include(entity => entity.Items)
                .AsSplitQuery()
                .FirstOrDefaultAsync(entity => entity.Number == number, cancellationToken);

            return version is not null
                ? version
                : CatalogErrors.VersionNotFound;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while getting version {Number}", number);
            return GeneralErrors.Failure("Process get version");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting version {Number}", number);
            return GeneralErrors.Failure();
        }
    }

    public async Task<Result<long?, Error>> GetLatestNumberAsync(CancellationToken cancellationToken)
    {
        try
        {
            long? max = await dbContext.CatalogVersions
                .MaxAsync(version => (long?)version.Number, cancellationToken);

            return Result.Success<long?, Error>(max);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while getting latest version");
            return GeneralErrors.Failure("Process get latest version");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting latest version");
            return GeneralErrors.Failure();
        }
    }

    public Task<Result<Guid, Error>> AddAsync(CatalogVersion version, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.CatalogVersions.Add(version);
            return Task.FromResult<Result<Guid, Error>>(version.Id);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while creating version");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure("Process create version"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating version");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure());
        }
    }
}
