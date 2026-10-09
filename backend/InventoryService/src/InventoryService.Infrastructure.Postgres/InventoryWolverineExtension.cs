using Wolverine;

namespace InventoryService.Infrastructure.Postgres;

public sealed class InventoryWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options) =>
        options.CodeGeneration.AlwaysUseServiceLocationFor<InventoryServiceDbContext>();
}
