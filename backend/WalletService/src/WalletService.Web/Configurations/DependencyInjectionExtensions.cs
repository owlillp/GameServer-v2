using Shared.Authentication;
using Shared.Framework.Cors;
using Shared.Framework.Endpoints;
using Shared.Framework.HealthChecks;
using Shared.Framework.Logging;
using Shared.Framework.OpenApi;
using Shared.Messaging;
using WalletService.Core;
using WalletService.Core.Messaging;
using WalletService.Domain.Authorization;
using WalletService.Infrastructure.Postgres;
using WalletService.Web.Grpc;

namespace WalletService.Web.Configurations;

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
            builder.Services.Configure<GrpcAuthOptions>(configuration.GetSection(GrpcAuthOptions.SECTION_NAME));
            builder.Services.AddSingleton<ServiceTokenInterceptor>();
            builder.Services.AddGrpc(options => options.Interceptors.Add<ServiceTokenInterceptor>());
            builder.Services.AddOpenApiSpec(Constants.SERVICE_NAME, Constants.VERSION);
            builder.Services.AddPlatformHealthChecks()
                .AddDbContextCheck<WalletServiceDbContext>(POSTGRESQL_HEALTH_CHECK, tags: [HealthCheckExtensions.READY_TAG])
                .AddRabbitMqCheck(configuration, tags: [HealthCheckExtensions.READY_TAG]);
            builder.Services.AddEndpoints(typeof(DependencyInjectionExtensions).Assembly);

            builder.Services.AddCore();
            builder.Services.AddInfrastructurePostgres(configuration);
            builder.Services.AddHostedService<WalletService.Web.Services.HoldExpiryService>();

            return builder;
        }
    }
}
