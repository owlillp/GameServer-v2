using Microsoft.EntityFrameworkCore;
using StoreService.Core.Abstractions;
using StoreService.Domain.Offers;
using StoreService.Domain.Orders;
using Wolverine.EntityFrameworkCore;

namespace StoreService.Infrastructure.Postgres;

public class StoreServiceDbContext(DbContextOptions<StoreServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<OfferItem> OfferItems => Set<OfferItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public IQueryable<Offer> OffersRead => Offers.AsQueryable().AsNoTracking();

    public IQueryable<Order> OrdersRead => Orders.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreServiceDbContext).Assembly);
    }
}
