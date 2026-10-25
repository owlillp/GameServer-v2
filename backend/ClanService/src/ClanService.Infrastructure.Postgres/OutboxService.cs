using ClanService.Core.Abstractions;
using Wolverine.EntityFrameworkCore;

namespace ClanService.Infrastructure.Postgres;

public sealed class OutboxService(IDbContextOutbox<ClanServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);
}
