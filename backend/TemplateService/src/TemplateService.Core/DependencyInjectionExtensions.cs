using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;

namespace TemplateService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore()
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);

            return services;
        }
    }
}
