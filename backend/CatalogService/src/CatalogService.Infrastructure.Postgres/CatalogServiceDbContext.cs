using CatalogService.Core.Abstractions;
using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Postgres;

public class CatalogServiceDbContext(DbContextOptions<CatalogServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Item> Items => Set<Item>();

    public DbSet<ItemStat> ItemStats => Set<ItemStat>();

    public DbSet<ItemPrice> ItemPrices => Set<ItemPrice>();

    public DbSet<AvailabilityWindow> AvailabilityWindows => Set<AvailabilityWindow>();

    public DbSet<CatalogVersion> CatalogVersions => Set<CatalogVersion>();

    public DbSet<CatalogVersionItem> CatalogVersionItems => Set<CatalogVersionItem>();

    public IQueryable<Item> ItemsRead => Items.AsQueryable().AsNoTracking();

    public IQueryable<CatalogVersion> VersionsRead => CatalogVersions.AsQueryable().AsNoTracking();

    public IQueryable<CatalogVersionItem> VersionItemsRead => CatalogVersionItems.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogServiceDbContext).Assembly);
    }
}
