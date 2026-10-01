using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TemplateService.Infrastructure.Postgres;

public class TemplateServiceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TemplateServiceDbContext>
{
    private const string CONNECTION_STRING = "Host=design-time;Database=design;Username=postgres";

    public TemplateServiceDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TemplateDb")
                                  ?? CONNECTION_STRING;

        var optionsBuilder = new DbContextOptionsBuilder<TemplateServiceDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));

        return new TemplateServiceDbContext(optionsBuilder.Options);
    }
}
