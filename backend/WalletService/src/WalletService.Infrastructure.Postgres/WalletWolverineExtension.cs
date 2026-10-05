using Wolverine;

namespace WalletService.Infrastructure.Postgres;

public sealed class WalletWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options) =>
        options.CodeGeneration.AlwaysUseServiceLocationFor<WalletServiceDbContext>();
}
