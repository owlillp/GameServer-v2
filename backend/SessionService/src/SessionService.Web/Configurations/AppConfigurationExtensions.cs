using Scalar.AspNetCore;
using SessionService.Web.Grpc;
using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.HealthChecks;
using Shared.Framework.Logging;
using Shared.Framework.Middlewares;

namespace SessionService.Web.Configurations;

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
        app.MapGrpcService<GameServerSessionsGrpcService>().RequireAuthorization();
        app.MapEndpoints();
        app.MapEndpointGroups(typeof(AppConfigurationExtensions).Assembly);

        return app;
    }
}
