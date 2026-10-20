using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Orders.Queries.GetMyOrders;

public sealed record GetMyOrdersQuery(Guid AccountId, int Limit = 50) : IQuery;
