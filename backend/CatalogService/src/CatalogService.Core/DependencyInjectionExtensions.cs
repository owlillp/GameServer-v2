using CatalogService.Core.Cache;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;

namespace CatalogService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore(IConfiguration configuration)
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);
            services.AddCatalogCaching(configuration);

            return services;
        }

        private IServiceCollection AddCatalogCaching(IConfiguration configuration)
        {
            services.Configure<CacheOptions>(configuration.GetSection(nameof(CacheOptions)));
            var cacheOptions = configuration.GetSection(nameof(CacheOptions)).Get<CacheOptions>()
                               ?? new CacheOptions();

            if (cacheOptions.EnableRedisCache)
            {
                services.AddStackExchangeRedisCache(setup =>
                {
                    setup.Configuration = configuration.GetConnectionString("Redis");
                });
            }

            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    LocalCacheExpiration = TimeSpan.FromMinutes(cacheOptions.LocalCacheExpirationMinutes),
                    Expiration = TimeSpan.FromMinutes(cacheOptions.CacheExpirationMinutes),
                };
            });

            services.AddScoped<CatalogCacheInvalidator>();

            return services;
        }
    }
}
