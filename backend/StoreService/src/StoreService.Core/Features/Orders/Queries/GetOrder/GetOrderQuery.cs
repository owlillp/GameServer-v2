using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Orders.Queries.GetOrder;

public sealed record GetOrderQuery(Guid OrderId, Guid AccountId, bool AllowAnyAccount = false) : IQuery;
