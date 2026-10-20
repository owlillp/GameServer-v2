using CatalogService.Contracts.Communication;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;
using StoreService.Core.Configuration;
using StoreService.Core.Services;
using WalletService.Contracts.Communication;

namespace StoreService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore(IConfiguration configuration)
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.Configure<StoreOptions>(configuration.GetSection(StoreOptions.SECTION_NAME));

            services.AddCatalogServiceHttpCommunication(configuration);
            services.AddWalletServiceGrpcCommunication(configuration);

            services.AddHostedService<OrderTimeoutService>();

            return services;
        }
    }
}
