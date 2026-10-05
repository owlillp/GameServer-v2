using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProfileService.Infrastructure.Postgres;

public class ProfileDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ReadDbContext>
{
    private const string CONNECTION_STRING = "Host=design-time;Database=design;Username=postgres";

    public ReadDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ProfileDb")
            ?? CONNECTION_STRING;

        var optionsBuilder = new DbContextOptionsBuilder<ReadDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));

        return new ReadDbContext(optionsBuilder.Options);
    }
}
