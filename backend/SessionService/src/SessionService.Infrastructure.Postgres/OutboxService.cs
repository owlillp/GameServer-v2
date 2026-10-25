using SessionService.Core.Abstractions;
using Wolverine.EntityFrameworkCore;

namespace SessionService.Infrastructure.Postgres;

public sealed class OutboxService(IDbContextOutbox<SessionServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);
}
