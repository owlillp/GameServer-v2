using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;
using TemplateService.Core;
using TemplateService.Core.Authorization;
using TemplateService.Infrastructure.Postgres;

namespace TemplateService.Web.Configurations;

public static class DependencyInjectionExtensions
{
    private const string POSTGRESQL_HEALTH_CHECK = "postgresql";
    private const string AUTH_SECTION_NAME = "Authentication";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDependency(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            builder.AddSerilogLogging(Constants.SERVICE_NAME);

            builder.Services.AddPlatformAuthentication(
                configuration,
                TemplateRolePermissions.Map,
                sectionName: AUTH_SECTION_NAME);

            builder.Services.AddRateLimiter(_ => { });
            builder.Services.AddFrameworkCors(configuration);
            builder.Services.AddOpenApiSpec(Constants.SERVICE_NAME, Constants.VERSION);
            builder.Services.AddHealthChecks().AddDbContextCheck<TemplateServiceDbContext>(POSTGRESQL_HEALTH_CHECK);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);

            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);

            return builder;
        }
    }
}
