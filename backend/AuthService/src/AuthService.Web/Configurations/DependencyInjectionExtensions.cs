using AuthService.Core;
using AuthService.Infrastructure.Postgres;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;

namespace AuthService.Web.Configurations;

public static class DependencyInjectionExtensions
{
    private const string POSTGRESQL_HEALTH_CHECK = "postgresql";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDependency(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            builder.AddSerilogLogging(Constants.SERVICE_NAME);

            // AuthService — issuer токенов. JWT-валидация (AddPlatformAuthentication)
            // появится вместе с OIDC; пока сервис не проверяет токены.
            builder.Services.AddRateLimiter(_ => { });
            builder.Services.AddFrameworkCors(configuration);
            builder.Services.AddOpenApiSpec(Constants.SERVICE_NAME, Constants.VERSION);
            builder.Services.AddHealthChecks().AddDbContextCheck<AuthServiceDbContext>(POSTGRESQL_HEALTH_CHECK);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);

            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);

            return builder;
        }
    }
}
