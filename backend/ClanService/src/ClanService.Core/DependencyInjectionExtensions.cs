using ClanService.Core.Abstractions;
using ClanService.Core.Configuration;
using ClanService.Core.Messaging.Publishers;
using ClanService.Core.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Core.Abstractions;

namespace ClanService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore()
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.AddOptions<ClanOptions>()
                .BindConfiguration(ClanOptions.SECTION_NAME)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IClanEventPublisher, ClanEventPublisher>();
            services.AddScoped<ClanMembership>();
            services.AddScoped<ClanPointsService>();

            services.AddHostedService<ClanFeeSweeper>();

            return services;
        }
    }
}
