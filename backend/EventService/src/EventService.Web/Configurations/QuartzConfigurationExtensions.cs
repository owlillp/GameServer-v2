using System.Data.Common;
using EventService.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Quartz;

namespace EventService.Web.Configurations;

public static class QuartzConfigurationExtensions
{
    private const string NPGSQL_PROVIDER = "Npgsql";

    public static IServiceCollection AddEventQuartz(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(
                EventService.Core.Constants.ConnectionStringNames.DATABASE)
            ?? throw new InvalidOperationException(
                $"Connection string '{EventService.Core.Constants.ConnectionStringNames.DATABASE}' is not configured.");

        EventsOptions options = configuration
            .GetSection(EventsOptions.SECTION_NAME)
            .Get<EventsOptions>() ?? new EventsOptions();

        if (!DbProviderFactories.GetProviderInvariantNames().Contains(NPGSQL_PROVIDER, StringComparer.OrdinalIgnoreCase))
        {
            DbProviderFactories.RegisterFactory(NPGSQL_PROVIDER, NpgsqlFactory.Instance);
        }

        services.AddQuartz(quartz =>
        {
            quartz.SchedulerId = "EventService";
            quartz.SchedulerName = "EventServiceScheduler";
            quartz.MisfireThreshold = TimeSpan.FromSeconds(Math.Max(1, options.QuartzMisfireThresholdSeconds));

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
