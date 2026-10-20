using Wolverine;

namespace EventService.Infrastructure.Postgres;

public sealed class EventWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options) =>
        options.CodeGeneration.AlwaysUseServiceLocationFor<EventServiceDbContext>();
}
