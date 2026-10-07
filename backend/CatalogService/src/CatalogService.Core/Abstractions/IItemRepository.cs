using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Abstractions;

public interface IItemRepository
{
    Task<Result<Item, Error>> GetByIdAsync(Guid itemId, CancellationToken cancellationToken);

    Task<Result<Item, Error>> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Item>, Error>> ListAsync(bool includeArchived, CancellationToken cancellationToken);

    Task<Result<Guid, Error>> AddAsync(Item item, CancellationToken cancellationToken);

    Task<Result<int, Error>> AddRangeAsync(IEnumerable<Item> items, CancellationToken cancellationToken);
}
