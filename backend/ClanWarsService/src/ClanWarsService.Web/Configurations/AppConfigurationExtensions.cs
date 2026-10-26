using Scalar.AspNetCore;
using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.HealthChecks;
using Shared.Framework.Logging;
using Shared.Framework.Middlewares;
using Shared.Observability;

namespace ClanWarsService.Web.Configurations;

public static class AppConfigurationExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseExceptionMiddleware();
        app.UseRequestCorrelationId();
        app.UseSerilogHttpRequestLogging();
        app.UseRouting();
        app.ConfigureCors();
        app.UsePlatformAuthentication();

        if (!app.Environment.IsProduction())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }

        app.MapPlatformHealthChecks();
        app.MapPlatformMetrics();
        app.MapEndpoints();
        app.MapEndpointGroups(typeof(AppConfigurationExtensions).Assembly);

        return app;
    }
}
