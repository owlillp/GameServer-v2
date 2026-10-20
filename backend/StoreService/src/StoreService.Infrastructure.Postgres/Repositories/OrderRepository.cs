using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel.Errors;
using StoreService.Core.Abstractions;
using StoreService.Domain.Orders;

namespace StoreService.Infrastructure.Postgres.Repositories;

public sealed class OrderRepository(StoreServiceDbContext dbContext) : IOrderRepository
{
    public async Task<Result<Order, Error>> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);

        return order is not null ? order : GeneralErrors.NotFound();
    }

    public Task<Order?> FindByIdempotencyKeyAsync(
        Guid accountId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(
                order => order.AccountId == accountId && order.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Include(order => order.Items)
            .Where(order => order.AccountId == accountId)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListPendingAsync(
        DateTime createdBefore,
        CancellationToken cancellationToken) =>
        await dbContext.Orders
            .Where(order => order.CreatedAt <= createdBefore
                && (order.Status == OrderStatus.PENDING
                    || order.Status == OrderStatus.RESERVED
                    || order.Status == OrderStatus.GRANT_REQUESTED))
            .ToListAsync(cancellationToken);

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        dbContext.Orders.Add(order);
        return Task.CompletedTask;
    }
}
