using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Core.Messaging.Publishers;
using ClanWarsService.Core.Scheduling;
using ClanWarsService.Core.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Core.Abstractions;

namespace ClanWarsService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore()
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.AddOptions<ClanWarsOptions>()
                .BindConfiguration(ClanWarsOptions.SECTION_NAME)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IClanWarsEventPublisher, ClanWarsEventPublisher>();
            services.AddScoped<ISeasonScheduler, SeasonScheduler>();
            services.AddScoped<SeasonLifecycle>();
            services.AddScoped<ScoreService>();
            services.AddScoped<LeaderboardSync>();
            services.AddScoped<LeaderboardReader>();
            services.AddSingleton<LeaderboardSyncSignal>();

            services.AddHostedService<SeasonSchedulingInitializer>();
            services.AddHostedService<LeaderboardSynchronizer>();

            return services;
        }
    }
}
