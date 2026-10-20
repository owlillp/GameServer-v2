using StoreService.Domain.Offers;
using StoreService.Domain.Orders;

namespace StoreService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Offer> OffersRead { get; }

    IQueryable<Order> OrdersRead { get; }
}
