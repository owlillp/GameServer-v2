using ClanWarsService.Core.Abstractions;
using ClanWarsService.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Core.Database;
using Wolverine;

namespace ClanWarsService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionName = Core.Constants.ConnectionStringNames.DATABASE;

        string connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        var dataSource = new NpgsqlDataSourceBuilder(connectionString)
            { Name = connectionName }.Build();

        services.AddSingleton(dataSource);
        services.AddDbContextPool<ClanWarsServiceDbContext>((serviceProvider, options) =>
        {
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

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

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IOutboxService, OutboxService>();

        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IRosterRepository, RosterRepository>();
        services.AddScoped<IScoreRepository, ScoreRepository>();

        return services;
    }
}
