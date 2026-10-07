using CatalogService.Core.Abstractions;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;

namespace CatalogService.Infrastructure.Postgres.Repositories;

public sealed class ItemRepository(
    ILogger<ItemRepository> logger,
    CatalogServiceDbContext dbContext) : IItemRepository
{
    public async Task<Result<Item, Error>> GetByIdAsync(Guid itemId, CancellationToken cancellationToken)
    {
        try
        {
            Item? item = await IncludeChildren(dbContext.Items)
                .FirstOrDefaultAsync(entity => entity.Id == itemId, cancellationToken);

            return item is not null
                ? item
                : CatalogErrors.ItemNotFound(itemId);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while getting item {ItemId}", itemId);
            return GeneralErrors.Failure("Process get item");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting item {ItemId}", itemId);
            return GeneralErrors.Failure();
        }
    }

    public async Task<Result<Item, Error>> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            Item? item = await IncludeChildren(dbContext.Items)
                .FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

            return item is not null
                ? item
                : GeneralErrors.NotFound();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while getting item {Code}", code);
            return GeneralErrors.Failure("Process get item");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting item {Code}", code);
            return GeneralErrors.Failure();
        }
    }

    public async Task<Result<IReadOnlyList<Item>, Error>> ListAsync(bool includeArchived, CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<Item> query = IncludeChildren(dbContext.Items);

            if (!includeArchived)
            {
                query = query.Where(item => item.Status != ItemStatus.Archived);
            }

            List<Item> items = await query.ToListAsync(cancellationToken);
            return Result.Success<IReadOnlyList<Item>, Error>(items);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while listing items");
            return GeneralErrors.Failure("Process list items");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while listing items");
            return GeneralErrors.Failure();
        }
    }

    public Task<Result<Guid, Error>> AddAsync(Item item, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Items.Add(item);
            return Task.FromResult<Result<Guid, Error>>(item.Id);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while creating item");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure("Process create item"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating item");
            return Task.FromResult<Result<Guid, Error>>(GeneralErrors.Failure());
        }
    }

    public Task<Result<int, Error>> AddRangeAsync(IEnumerable<Item> items, CancellationToken cancellationToken)
    {
        try
        {
            List<Item> materialized = [.. items];
            dbContext.Items.AddRange(materialized);
            return Task.FromResult<Result<int, Error>>(materialized.Count);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while creating items");
            return Task.FromResult<Result<int, Error>>(GeneralErrors.Failure("Process create items"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating items");
            return Task.FromResult<Result<int, Error>>(GeneralErrors.Failure());
        }
    }

    private static IQueryable<Item> IncludeChildren(IQueryable<Item> query) =>
        query
            .Include(item => item.Stats)
            .Include(item => item.Prices)
            .Include(item => item.Availability)
            .AsSplitQuery();
}
