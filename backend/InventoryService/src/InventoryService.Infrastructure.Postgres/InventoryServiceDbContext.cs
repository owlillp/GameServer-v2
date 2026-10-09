using InventoryService.Core.Abstractions;
using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Postgres;

public class InventoryServiceDbContext(DbContextOptions<InventoryServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<OwnedItem> OwnedItems => Set<OwnedItem>();

    public DbSet<OutfitEntry> OutfitEntries => Set<OutfitEntry>();

    public DbSet<OwnedCar> OwnedCars => Set<OwnedCar>();

    public DbSet<CarInstallation> CarInstallations => Set<CarInstallation>();

    public DbSet<GrantReceipt> GrantReceipts => Set<GrantReceipt>();

    public IQueryable<Inventory> InventoriesRead => Inventories.AsQueryable().AsNoTracking();

    public IQueryable<GrantReceipt> GrantReceiptsRead => GrantReceipts.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryServiceDbContext).Assembly);
    }
}
