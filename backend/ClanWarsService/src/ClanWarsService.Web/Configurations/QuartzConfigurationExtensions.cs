using System.Data.Common;
using ClanWarsService.Core.Configuration;
using Npgsql;
using Quartz;

namespace ClanWarsService.Web.Configurations;

public static class QuartzConfigurationExtensions
{
    private const string NPGSQL_PROVIDER = "Npgsql";

    /// <summary>
    /// Persistent clustered JobStore in the service database: season boundaries survive restarts,
    /// fire on one replica only, and missed ones fire on startup.
    /// </summary>
    public static IServiceCollection AddSeasonQuartz(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ClanWarsService.Core.Constants.ConnectionStringNames.DATABASE)
            ?? throw new InvalidOperationException(
                $"Connection string '{ClanWarsService.Core.Constants.ConnectionStringNames.DATABASE}' is not configured.");

        ClanWarsOptions options = configuration.GetSection(ClanWarsOptions.SECTION_NAME).Get<ClanWarsOptions>() ?? new ClanWarsOptions();

        if (!DbProviderFactories.GetProviderInvariantNames().Contains(NPGSQL_PROVIDER, StringComparer.OrdinalIgnoreCase))
        {
            DbProviderFactories.RegisterFactory(NPGSQL_PROVIDER, NpgsqlFactory.Instance);
        }

        services.AddQuartz(quartz =>
        {
            quartz.SchedulerId = "AUTO";
            quartz.SchedulerName = "ClanWarsScheduler";
            quartz.MisfireThreshold = TimeSpan.FromSeconds(options.QuartzMisfireThresholdSeconds);

            quartz.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UsePostgres(connectionString);
                store.UseNewtonsoftJsonSerializer();

                if (options.QuartzClustered)
                {
                    store.UseClustering();
                }
            });
        });

        services.AddQuartzHostedService(hosted => hosted.WaitForJobsToComplete = true);

        return services;
    }
}
