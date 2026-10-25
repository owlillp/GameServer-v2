using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shared.Communication;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServiceTokenProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SECTION_NAME));
        services.AddHttpClient(Constants.SERVICE_AUTH_HTTP_CLIENT);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IServiceTokenProvider, ServiceTokenProvider>();

        return services;
    }
}
