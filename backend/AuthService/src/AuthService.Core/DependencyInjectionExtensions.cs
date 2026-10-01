using AuthService.Core.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;

namespace AuthService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore()
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.AddScoped<PlatformSyncService>();

            return services;
        }
    }
}
