using FluentValidation;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Core.Messaging;
using MatchmakingService.Core.Messaging.Publishers;
using MatchmakingService.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;

namespace MatchmakingService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore(IConfiguration configuration)
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.Configure<MatchmakingOptions>(configuration.GetSection(MatchmakingOptions.SECTION_NAME));

            services.AddScoped<IMatchmakingEventPublisher, MatchmakingEventPublisher>();
            services.AddScoped<MatchRewardDispatcher>();
            services.AddScoped<MatchFinalizer>();

            services.AddHostedService<MatchmakingCoordinator>();
            services.AddHostedService<LeaderboardInitializer>();

            return services;
        }
    }
}
