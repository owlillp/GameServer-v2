using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using ProfileService.Core.Abstractions;
using Shared.Core.Database;

namespace ProfileService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(Constants.PROFILE_DB_CONNECTION_NAME)
            ?? throw new InvalidOperationException($"Connection string '{Constants.PROFILE_DB_CONNECTION_NAME}' is not configured.");

        var dataSource = new NpgsqlDataSourceBuilder(connectionString)
            { Name = Constants.PROFILE_DB_CONNECTION_NAME }.Build();

        services.AddSingleton(dataSource);
        services.AddDbContextPool<ReadDbContext>((sp, options) =>
        {
            var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            options.UseNpgsql(
                dataSource,
                npgsql => npgsql.MigrationsHistoryTable(Constants.EF_MIGRATION_HISTORY, Constants.SCHEMA));
            options.UseLoggerFactory(loggerFactory);

            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IReadDbContext, ReadDbContext>();
        services.AddScoped<ITransactionManager, TransactionManager>();

        return services;
    }
}
