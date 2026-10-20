using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Communication;
using Shared.Grpc.Wallet;
using WalletService.Contracts.Communication.GrpcCommunication;

namespace WalletService.Contracts.Communication;

public static class WalletServiceExtensions
{
    public static IServiceCollection AddWalletServiceGrpcCommunication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WalletServiceOptions>(configuration.GetSection(WalletServiceOptions.SECTION_NAME));
        services.AddServiceTokenProvider(configuration);

        string grpcAddress = configuration[$"{WalletServiceOptions.SECTION_NAME}:{nameof(WalletServiceOptions.GrpcAddress)}"]
            ?? throw new InvalidOperationException(
                $"Configuration '{WalletServiceOptions.SECTION_NAME}:{nameof(WalletServiceOptions.GrpcAddress)}' is not set.");

        services.AddSingleton(GrpcChannel.ForAddress(grpcAddress));
        services.AddSingleton(serviceProvider =>
            new WalletReservation.WalletReservationClient(
                serviceProvider.GetRequiredService<GrpcChannel>()));
        services.AddScoped<IWalletClient, WalletGrpcClient>();

        return services;
    }
}
