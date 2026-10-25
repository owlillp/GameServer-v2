using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Core.GameServers;
using SessionService.Core.Messaging.Publishers;
using SessionService.Core.Services;
using Shared.Core.Abstractions;

namespace SessionService.Core;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCore()
        {
            services.AddHandlers(typeof(DependencyInjectionExtensions).Assembly);
            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);

            services.AddValidatedOptions<SessionOptions>(SessionOptions.SECTION_NAME);
            services.AddValidatedOptions<AntiCheatOptions>(AntiCheatOptions.SECTION_NAME);
            services.AddValidatedOptions<JoinTicketOptions>(JoinTicketOptions.SECTION_NAME);

            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<JoinTicketService>();

            services.AddScoped<ISessionEventPublisher, SessionEventPublisher>();
            services.AddScoped<IGameServerAllocator, StaticPoolAllocator>();
            services.AddScoped<SessionPlacement>();
            services.AddScoped<GameServerOperations>();

            services.AddHostedService<SessionLivenessSweeper>();

            return services;
        }

        private void AddValidatedOptions<TOptions>(string sectionName)
            where TOptions : class =>
            services.AddOptions<TOptions>()
                .BindConfiguration(sectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
    }
}
