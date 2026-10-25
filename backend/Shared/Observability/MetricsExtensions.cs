using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Shared.Observability;

public static class MetricsExtensions
{
    public const string METRICS_PATH = "/metrics";

    /// <summary>
    /// OpenTelemetry metrics with a Prometheus scrape endpoint: HTTP server metrics plus the
    /// service's own meters.
    /// </summary>
    public static IServiceCollection AddPlatformMetrics(this IServiceCollection services, string serviceName, params string[] meters)
    {
        services.AddSingleton<SampledGauges>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddMeter([SampledGauges.METER_NAME, .. meters])
                .AddAspNetCoreInstrumentation()
                .AddPrometheusExporter());

        return services;
    }

    /// <summary>
    /// Publishes <c>/metrics</c> for Prometheus. Served on the internal port only: nginx does not proxy it.
    /// </summary>
    public static IEndpointRouteBuilder MapPlatformMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPrometheusScrapingEndpoint(METRICS_PATH).AllowAnonymous();
        return endpoints;
    }
}
