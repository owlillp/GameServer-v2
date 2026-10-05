using ProfileService.Core;
using ProfileService.Domain.Authorization;
using ProfileService.Infrastructure.Postgres;
using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;

namespace ProfileService.Web.Configurations;

public static class DependencyInjectionExtensions
{
    private const string POSTGRESQL_HEALTH_CHECK = "postgresql";
    private const string AUTHENTICATION_SECTION_NAME = "Authentication";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDependency(IConfiguration configuration)
        {
            builder.AddSerilogLogging(Constants.SERVICE_NAME);

            builder.Services.AddPlatformAuthentication(configuration, RolePermissions.Map, AUTHENTICATION_SECTION_NAME);
            builder.Services.AddFrameworkCors(configuration);
            builder.Services.AddOpenApiSpec(Constants.SERVICE_NAME, Constants.VERSION);
            builder.Services.AddHealthChecks().AddDbContextCheck<ReadDbContext>(POSTGRESQL_HEALTH_CHECK);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);

            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);

            return builder;
        }
    }
}
