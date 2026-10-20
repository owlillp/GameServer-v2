using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using StoreService.Core.Features.Orders.Models;
using StoreService.Domain;

namespace StoreService.Core.Features.Orders.Queries.GetOrder;

public sealed class GetOrderHandler(IOrderRepository orderRepository)
    : IQueryHandler<OrderDto, GetOrderQuery>
{
    public async Task<Result<OrderDto, Error>> Handle(
        GetOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var orderResult = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (orderResult.IsFailure)
        {
            return StoreErrors.OrderNotFound;
        }

        if (!query.AllowAnyAccount && orderResult.Value.AccountId != query.AccountId)
        {
            return StoreErrors.OrderNotFound;
        }

        return OrderMapper.ToDto(orderResult.Value);
    }
}
