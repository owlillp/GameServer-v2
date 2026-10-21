using EventService.Core.Configuration;
using EventService.Core.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;

namespace EventService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore(IConfiguration configuration)
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.Configure<EventsOptions>(configuration.GetSection(EventsOptions.SECTION_NAME));

            services.AddScoped<IEventScheduler, EventScheduler>();
            services.AddHostedService<EventSchedulingInitializer>();

            return services;
        }
    }
}
