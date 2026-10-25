using MatchmakingService.Core;
using MatchmakingService.Core.Messaging;
using MatchmakingService.Domain.Authorization;
using MatchmakingService.Infrastructure.Postgres;
using MatchmakingService.Infrastructure.Redis;
using MatchmakingService.Web.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.HealthChecks;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;
using Shared.Messaging;
using WalletService.Contracts.Communication;

namespace MatchmakingService.Web.Configurations;

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
            builder.Services.AddPlatformHealthChecks()
                .AddDbContextCheck<MatchmakingServiceDbContext>(POSTGRESQL_HEALTH_CHECK, tags: [HealthCheckExtensions.READY_TAG])
                .AddRabbitMqCheck(configuration, tags: [HealthCheckExtensions.READY_TAG])
                .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);

            builder.Services.AddWalletServiceGrpcCommunication(configuration);
            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);
            builder.Services.AddInfrastructureRedis(configuration);

            return builder;
        }
    }
}
