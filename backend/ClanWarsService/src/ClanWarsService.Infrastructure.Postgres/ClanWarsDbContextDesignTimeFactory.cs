using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClanWarsService.Infrastructure.Postgres;

public class ClanWarsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ClanWarsServiceDbContext>
{
    private const string CONNECTION_STRING = "Host=design-time;Database=design;Username=postgres";

    public ClanWarsServiceDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(
                $"ConnectionStrings__{Core.Constants.ConnectionStringNames.DATABASE}")
            ?? CONNECTION_STRING;

        var optionsBuilder = new DbContextOptionsBuilder<ClanWarsServiceDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));

        return new ClanWarsServiceDbContext(optionsBuilder.Options);
    }
}
