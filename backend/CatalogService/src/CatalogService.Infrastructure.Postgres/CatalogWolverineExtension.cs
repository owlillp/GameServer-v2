using Wolverine;

namespace CatalogService.Infrastructure.Postgres;

public sealed class CatalogWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options) =>
        options.CodeGeneration.AlwaysUseServiceLocationFor<CatalogServiceDbContext>();
}
