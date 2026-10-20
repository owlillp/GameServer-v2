using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using StoreService.Domain.Orders;

namespace StoreService.Core.Abstractions;

public interface IOrderRepository
{
    Task<Result<Order, Error>> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Order?> FindByIdempotencyKeyAsync(
        Guid accountId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListPendingAsync(DateTime createdBefore, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);
}
