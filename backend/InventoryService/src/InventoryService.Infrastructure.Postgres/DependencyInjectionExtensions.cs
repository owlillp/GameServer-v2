using InventoryService.Core.Abstractions;
using InventoryService.Core.Messaging;
using InventoryService.Core.Messaging.Publishers;
using InventoryService.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Core.Database;
using Wolverine;

namespace InventoryService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionName = InventoryService.Core.Constants.ConnectionStringNames.DATABASE;

        string connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        var dataSource = new NpgsqlDataSourceBuilder(connectionString)
            { Name = connectionName }.Build();

        services.AddSingleton(dataSource);
        services.AddDbContextPool<InventoryServiceDbContext>((sp, options) =>
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

        services.AddScoped<IReadDbContext>(serviceProvider => serviceProvider.GetRequiredService<InventoryServiceDbContext>());
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryEventPublisher, InventoryEventPublisher>();
        services.AddScoped<IStorePurchaseResultPublisher, StorePurchaseResultPublisher>();
        services.AddScoped<IEventRewardResultPublisher, EventRewardResultPublisher>();
        services.AddScoped<IMatchRewardResultPublisher, MatchRewardResultPublisher>();
        services.AddScoped<ISeasonRewardResultPublisher, SeasonRewardResultPublisher>();

        services.AddSingleton<IWolverineExtension, InventoryWolverineExtension>();

        return services;
    }
}
