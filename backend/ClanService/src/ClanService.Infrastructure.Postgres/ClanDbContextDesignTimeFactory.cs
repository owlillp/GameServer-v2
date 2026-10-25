using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClanService.Infrastructure.Postgres;

public class ClanDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ClanServiceDbContext>
{
    private const string CONNECTION_STRING = "Host=design-time;Database=design;Username=postgres";

    public ClanServiceDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(
                $"ConnectionStrings__{Core.Constants.ConnectionStringNames.DATABASE}")
            ?? CONNECTION_STRING;

        var optionsBuilder = new DbContextOptionsBuilder<ClanServiceDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));

        return new ClanServiceDbContext(optionsBuilder.Options);
    }
}
