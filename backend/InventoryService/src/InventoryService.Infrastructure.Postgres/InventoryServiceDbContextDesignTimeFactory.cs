using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryService.Infrastructure.Postgres;

public class InventoryServiceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<InventoryServiceDbContext>
{
    private const string CONNECTION_STRING = "Host=design-time;Database=design;Username=postgres";

    public InventoryServiceDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(
                $"ConnectionStrings__{InventoryService.Core.Constants.ConnectionStringNames.DATABASE}")
            ?? CONNECTION_STRING;

        var optionsBuilder = new DbContextOptionsBuilder<InventoryServiceDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));

        return new InventoryServiceDbContext(optionsBuilder.Options);
    }
}
