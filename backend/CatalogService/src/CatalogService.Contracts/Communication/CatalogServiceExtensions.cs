using System.Net;
using CatalogService.Contracts.Communication.HttpCommunication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Shared.Communication;

namespace CatalogService.Contracts.Communication;

public static class CatalogServiceExtensions
{
    public static IServiceCollection AddCatalogServiceHttpCommunication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CatalogServiceOptions>(configuration.GetSection(CatalogServiceOptions.SECTION_NAME));
        services.AddServiceTokenProvider(configuration);

        services.AddHttpClient<ICatalogClient, CatalogHttpClient>((serviceProvider, client) =>
        {
            CatalogServiceOptions options = serviceProvider
                .GetRequiredService<IOptions<CatalogServiceOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.Url);
        })
        .AddResilienceHandler("catalog-service", static (builder, context) =>
        {
            CatalogServiceOptions options = context.ServiceProvider
                .GetRequiredService<IOptions<CatalogServiceOptions>>()
                .Value;

            builder.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => IsTransientHttpStatus(response.StatusCode)),
            });
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(10),
                FailureRatio = 0.5,
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => IsTransientHttpStatus(response.StatusCode)),
            });
        });

        return services;
    }

    private static bool IsTransientHttpStatus(HttpStatusCode statusCode) =>
        statusCode is
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
}
