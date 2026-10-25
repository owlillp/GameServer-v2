using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Communication;
using Shared.Grpc.Session;

namespace SessionService.Contracts.Communication;

public static class SessionServiceExtensions
{
    public static IServiceCollection AddSessionServiceGrpcCommunication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SessionServiceOptions>(configuration.GetSection(SessionServiceOptions.SECTION_NAME));
        services.AddServiceTokenProvider(configuration);

        string grpcAddress = configuration[$"{SessionServiceOptions.SECTION_NAME}:{nameof(SessionServiceOptions.GrpcAddress)}"]
            ?? throw new InvalidOperationException(
                $"Configuration '{SessionServiceOptions.SECTION_NAME}:{nameof(SessionServiceOptions.GrpcAddress)}' is not set.");

        // Own channel, not a GrpcChannel registration: other clients (wallet) register theirs.
        GrpcChannel channel = GrpcChannel.ForAddress(grpcAddress);
        services.AddSingleton(new MatchSessions.MatchSessionsClient(channel));
        services.AddScoped<ISessionTicketClient, SessionTicketGrpcClient>();

        return services;
    }
}
