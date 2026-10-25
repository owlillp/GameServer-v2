using ClanWarsService.Core.Abstractions;
using Wolverine.EntityFrameworkCore;

namespace ClanWarsService.Infrastructure.Postgres;

public sealed class OutboxService(IDbContextOutbox<ClanWarsServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);
}
