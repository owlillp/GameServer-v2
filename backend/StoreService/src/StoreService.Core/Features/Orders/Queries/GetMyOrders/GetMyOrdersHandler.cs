using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using StoreService.Core.Features.Orders.Models;
using StoreService.Domain.Orders;

namespace StoreService.Core.Features.Orders.Queries.GetMyOrders;

public sealed class GetMyOrdersHandler(IOrderRepository orderRepository)
    : IQueryHandler<IReadOnlyList<OrderDto>, GetMyOrdersQuery>
{
    public async Task<Result<IReadOnlyList<OrderDto>, Error>> Handle(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Order> orders = await orderRepository.ListByAccountAsync(
            query.AccountId,
            cancellationToken);

        int limit = query.Limit <= 0 ? 50 : Math.Min(query.Limit, 200);

        return orders
            .OrderByDescending(order => order.CreatedAt)
            .Take(limit)
            .Select(OrderMapper.ToDto)
            .ToList();
    }
}
