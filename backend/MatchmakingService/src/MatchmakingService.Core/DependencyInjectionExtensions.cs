using FluentValidation;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Core.Messaging;
using MatchmakingService.Core.Messaging.Publishers;
using MatchmakingService.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Core.Abstractions;

namespace MatchmakingService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore()
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.AddOptions<MatchmakingOptions>()
                .BindConfiguration(MatchmakingOptions.SECTION_NAME)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IMatchmakingEventPublisher, MatchmakingEventPublisher>();
            services.AddScoped<MatchRewardDispatcher>();
            services.AddScoped<MatchFinalizer>();
            services.AddScoped<MatchRecovery>();
            services.AddScoped<LeaderboardSync>();
            services.AddSingleton<LeaderboardSyncSignal>();

            services.AddHostedService<MatchmakingCoordinator>();
            services.AddHostedService<LeaderboardSynchronizer>();

            return services;
        }
    }
}
