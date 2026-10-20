using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StoreService.Infrastructure.Postgres;

public class StoreDbContextDesignTimeFactory : IDesignTimeDbContextFactory<StoreServiceDbContext>
{
    private const string CONNECTION_STRING = "Host=design-time;Database=design;Username=postgres";

    public StoreServiceDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(
                $"ConnectionStrings__{StoreService.Core.Constants.ConnectionStringNames.DATABASE}")
            ?? CONNECTION_STRING;

        var optionsBuilder = new DbContextOptionsBuilder<StoreServiceDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));

        return new StoreServiceDbContext(optionsBuilder.Options);
    }
}
