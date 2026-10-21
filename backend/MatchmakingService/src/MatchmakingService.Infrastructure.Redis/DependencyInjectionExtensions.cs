using MatchmakingService.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MatchmakingService.Infrastructure.Redis;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionName = Core.Constants.ConnectionStringNames.REDIS;
        string connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<ILeaderboardStore, RedisLeaderboardStore>();

        return services;
    }
}
