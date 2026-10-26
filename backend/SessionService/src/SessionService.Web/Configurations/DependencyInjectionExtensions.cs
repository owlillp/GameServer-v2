using SessionService.Core;
using SessionService.Core.Messaging;
using SessionService.Domain.Authorization;
using SessionService.Infrastructure.Postgres;
using SessionService.Infrastructure.Redis;
using SessionService.Web.Grpc;
using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.HealthChecks;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;
using Shared.Messaging;
using Shared.Observability;

namespace SessionService.Web.Configurations;

public static class DependencyInjectionExtensions
{
    private const string POSTGRESQL_HEALTH_CHECK = "postgresql";
    private const string REDIS_HEALTH_CHECK = "redis";
    private const string AUTHENTICATION_SECTION_NAME = "Authentication";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDependency(IConfiguration configuration)
        {
            builder.AddSerilogLogging(Constants.SERVICE_NAME);
            builder.AddWolverine();

            builder.Services.AddPlatformAuthentication(configuration, RolePermissions.Map, AUTHENTICATION_SECTION_NAME);
            builder.Services.AddFrameworkCors(configuration);
            builder.Services.AddOptions<GameServerAuthOptions>()
                .BindConfiguration(GameServerAuthOptions.SECTION_NAME)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddOptions<ServiceClientAuthOptions>()
                .BindConfiguration(ServiceClientAuthOptions.SECTION_NAME)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddSingleton<GameServerAuthInterceptor>();
            builder.Services.AddGrpc(options => options.Interceptors.Add<GameServerAuthInterceptor>());
            builder.Services.AddOpenApiSpec(Constants.SERVICE_NAME, Constants.VERSION);

            // Redis is the live session registry here, so unlike matchmaking it is critical for readiness.
            builder.Services.AddPlatformHealthChecks()
                .AddDbContextCheck<SessionServiceDbContext>(POSTGRESQL_HEALTH_CHECK, tags: [HealthCheckExtensions.READY_TAG])
                .AddRabbitMqCheck(configuration, tags: [HealthCheckExtensions.READY_TAG])
                .AddCheck<RedisHealthCheck>(REDIS_HEALTH_CHECK, tags: [HealthCheckExtensions.READY_TAG]);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);
            builder.Services.AddPlatformMetrics(Constants.SERVICE_NAME, SessionService.Core.SessionMetrics.METER_NAME);
            builder.Services.AddHostedService<SessionMetricsSampler>();

            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);
            builder.Services.AddInfrastructureRedis(configuration);

            return builder;
        }
    }
}
