using Wolverine;

namespace StoreService.Infrastructure.Postgres;

public sealed class StoreWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options) =>
        options.CodeGeneration.AlwaysUseServiceLocationFor<StoreServiceDbContext>();
}
