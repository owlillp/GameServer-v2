using ClanWarsService.Core;
using ClanWarsService.Core.Messaging;
using ClanWarsService.Domain.Authorization;
using ClanWarsService.Infrastructure.Postgres;
using ClanWarsService.Infrastructure.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.HealthChecks;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;
using Shared.Messaging;

namespace ClanWarsService.Web.Configurations;

public static class DependencyInjectionExtensions
{
    private const string POSTGRESQL_HEALTH_CHECK = "postgresql";
    private const string AUTHENTICATION_SECTION_NAME = "Authentication";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDependency(IConfiguration configuration)
        {
            builder.AddSerilogLogging(Constants.SERVICE_NAME);
            builder.AddWolverine();

            builder.Services.AddPlatformAuthentication(configuration, RolePermissions.Map, AUTHENTICATION_SECTION_NAME);
            builder.Services.AddFrameworkCors(configuration);
            builder.Services.AddOpenApiSpec(Constants.SERVICE_NAME, Constants.VERSION);

            // Redis is a cache of the leaderboard: reads fall back to PostgreSQL, so it is not in readiness.
            builder.Services.AddPlatformHealthChecks()
                .AddDbContextCheck<ClanWarsServiceDbContext>(POSTGRESQL_HEALTH_CHECK, tags: [HealthCheckExtensions.READY_TAG])
                .AddRabbitMqCheck(configuration, tags: [HealthCheckExtensions.READY_TAG])
                .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);

            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);
            builder.Services.AddInfrastructureRedis(configuration);
            builder.Services.AddSeasonQuartz(configuration);

            return builder;
        }
    }
}
